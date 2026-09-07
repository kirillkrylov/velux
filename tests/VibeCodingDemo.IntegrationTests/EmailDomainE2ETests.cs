using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using FluentAssertions;
using NUnit.Framework;
using VibeCodingDemo.IntegrationTests.Infrastructure;

namespace VibeCodingDemo.IntegrationTests;

[TestFixture, NonParallelizable, AllureNUnit]
[AllureSuite("Contact email domain policy — live Creatio")]
public class EmailDomainE2ETests {
    private const string Setting = "UsrProhibitedEmailDomains";
    private const string EmailType = "ee1c85c3-cfcb-df11-9b2a-001d60e938c6";
    private HttpClient _client;
    private string _originalDomains;
    private string _phoneType;
    private readonly List<(string Entity, Guid Id)> _records = new();

    [OneTimeSetUp]
    public async Task Connect() {
        var settings = CreatioTestSettings.Load();
        var cookies = new CookieContainer();
        _client = new HttpClient(new HttpClientHandler { CookieContainer = cookies }) {
            BaseAddress = new Uri(settings.Url.ToString().TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(90)
        };
        if (settings.UsesAccessToken) {
            _client.DefaultRequestHeaders.Authorization = new("Bearer", settings.AccessToken);
        } else {
            using var login = await _client.PostAsJsonAsync("ServiceModel/AuthService.svc/Login", new { UserName = settings.Username, UserPassword = settings.Password });
            login.EnsureSuccessStatusCode();
            var body = JsonNode.Parse(await login.Content.ReadAsStringAsync());
            body["Code"].GetValue<int>().Should().Be(0, "authentication must succeed");
            string csrf = cookies.GetCookies(settings.Url)["BPMCSRF"]?.Value;
            if (csrf != null) { _client.DefaultRequestHeaders.Add("BPMCSRF", csrf); }
        }
        using var settingsResponse = await _client.PostAsJsonAsync("DataService/json/SyncReply/QuerySysSettings", new {
            sysSettingsNameCollection = new[] { Setting }
        });
        settingsResponse.EnsureSuccessStatusCode();
        var settingsBody = JsonNode.Parse(await settingsResponse.Content.ReadAsStringAsync());
        _originalDomains = settingsBody["values"][Setting]["value"].GetValue<string>();
        JsonArray types = await Rows("CommunicationType?$select=Id&$filter=Name eq 'Mobile phone'");
        types.Count.Should().BeGreaterThan(0);
        _phoneType = types[0]["Id"].GetValue<string>();
    }

    [SetUp]
    public async Task Arrange() {
        _records.Clear();
        await SetDomains("gmail.com; yahoo.com");
    }

    [TearDown]
    public async Task Cleanup() {
        // Restore policy first, then remove only IDs allocated by this test.
        try { await SetDomains(_originalDomains); }
        finally {
            foreach (var record in _records.AsEnumerable().Reverse()) {
                using var response = await _client.DeleteAsync($"odata/{record.Entity}({record.Id})");
                (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound).Should().BeTrue("test records must be cleaned up");
            }
        }
    }

    [OneTimeTearDown]
    public void Disconnect() => _client?.Dispose();

    [AllureStep("Configure the prohibited domain list")]
    private async Task SetDomains(string value) {
        using var response = await _client.PostAsJsonAsync("DataService/json/SyncReply/PostSysSettingsValues", new {
            isPersonal = false, sysSettingsValues = new Dictionary<string, string> { [Setting] = value }
        });
        response.EnsureSuccessStatusCode();
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        body["saveResult"][Setting].GetValue<bool>().Should().BeTrue();
    }

    private async Task<JsonArray> Rows(string query) {
        using var response = await _client.GetAsync("odata/" + query);
        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())["value"].AsArray();
    }

    private async Task<JsonObject> Read(string entity, Guid id) =>
        (await Rows($"{entity}?$filter=Id eq {id}")).SingleOrDefault()?.AsObject();

    [AllureStep("Attempt a real OData write and capture the outcome")]
    private async Task<bool> Write(HttpMethod method, string entity, Guid id, Dictionary<string, object> values) {
        using var request = new HttpRequestMessage(method, "odata/" + entity + (method == HttpMethod.Post ? "" : $"({id})"));
        if (method == HttpMethod.Post) { values["Id"] = id; _records.Add((entity, id)); }
        request.Content = JsonContent.Create(values);
        using var response = await _client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        AllureApi.AddAttachment("Write outcome", "text/plain", System.Text.Encoding.UTF8.GetBytes(
            $"{method} {entity}, record {id}\nHTTP {(int)response.StatusCode}\n{body}"));
        return response.IsSuccessStatusCode;
    }

    private async Task<Guid> CreateContact(string email = "allowed@example.org") {
        Guid id = Guid.NewGuid();
        (await Write(HttpMethod.Post, "Contact", id, new() { ["Name"] = "Velux E2E " + id, ["Email"] = email })).Should().BeTrue();
        return id;
    }

    private async Task<Guid> CreateCommunication(Guid contact, string number, string type = EmailType) {
        Guid id = Guid.NewGuid();
        (await Write(HttpMethod.Post, "ContactCommunication", id, new() {
            ["ContactId"] = contact, ["CommunicationTypeId"] = type, ["Number"] = number
        })).Should().BeTrue();
        return id;
    }

    [TestCase("person@gmail.com", false)]
    [TestCase("person@yahoo.com", false)]
    [TestCase("Person@GMAIL.COM", false)]
    [TestCase("person+tag@gmail.com", false)]
    [TestCase("person@example.org", true)]
    [TestCase("person@notgmail.com", true)]
    [TestCase("person@sub.gmail.com", true)]
    [TestCase("", true)]
    [AllureDescription("Create a Contact through the real OData save pipeline, then verify presence or absence in storage.")]
    public async Task ContactCreation(string email, bool allowed) {
        Guid id = Guid.NewGuid();
        (await Write(HttpMethod.Post, "Contact", id, new() { ["Name"] = "Velux E2E " + id, ["Email"] = email })).Should().Be(allowed);
        JsonObject row = await Read("Contact", id);
        (row != null).Should().Be(allowed, "the persisted outcome must agree with validation");
        if (allowed) { row["Email"].GetValue<string>().Should().Be(email); }
    }

    [TestCase("person@gmail.com", false)] [TestCase("new@example.org", true)] [TestCase("", true)]
    public async Task ContactEmailUpdate(string email, bool allowed) {
        Guid id = await CreateContact();
        (await Write(HttpMethod.Patch, "Contact", id, new() { ["Email"] = email })).Should().Be(allowed);
        (await Read("Contact", id))["Email"].GetValue<string>().Should().Be(allowed ? email : "allowed@example.org");
    }

    [TestCase("person@gmail.com", false)] [TestCase("person@yahoo.com", false)] [TestCase("person@example.org", true)]
    public async Task EmailCommunicationCreation(string email, bool allowed) {
        Guid contact = await CreateContact();
        Guid id = Guid.NewGuid();
        (await Write(HttpMethod.Post, "ContactCommunication", id, new() {
            ["ContactId"] = contact, ["CommunicationTypeId"] = EmailType, ["Number"] = email
        })).Should().Be(allowed);
        ((await Read("ContactCommunication", id)) != null).Should().Be(allowed);
    }

    [TestCase("bad@gmail.com", false)] [TestCase("new@example.org", true)]
    public async Task CommunicationNumberOnlyUpdate(string email, bool allowed) {
        Guid contact = await CreateContact();
        Guid id = await CreateCommunication(contact, "old@example.org");
        (await Write(HttpMethod.Patch, "ContactCommunication", id, new() { ["Number"] = email })).Should().Be(allowed);
        (await Read("ContactCommunication", id))["Number"].GetValue<string>().Should().Be(allowed ? email : "old@example.org");
    }

    [Test]
    public async Task NonEmailCommunicationIsUnaffectedAndCannotBeChangedToBlockedEmail() {
        Guid contact = await CreateContact();
        Guid id = await CreateCommunication(contact, "person@gmail.com", _phoneType);
        (await Write(HttpMethod.Patch, "ContactCommunication", id, new() { ["CommunicationTypeId"] = EmailType })).Should().BeFalse();
        (await Read("ContactCommunication", id))["CommunicationTypeId"].GetValue<string>().Should().Be(_phoneType);
    }

    [TestCase(false)] [TestCase(true)]
    [AllureDescription("A newly blocked existing email prevents unrelated Contact edits, including a blocked secondary communication. Removing the restriction takes effect without restart.")]
    public async Task ExistingBlockedEmailPreventsUnrelatedContactUpdate(bool secondary) {
        await SetDomains("");
        Guid contact = await CreateContact(secondary ? "allowed@example.org" : "legacy@gmail.com");
        if (secondary) {
            await CreateCommunication(contact, "first@example.org");
            await CreateCommunication(contact, "legacy@gmail.com");
        }
        string originalName = (await Read("Contact", contact))["Name"].GetValue<string>();
        await SetDomains("gmail.com");
        (await Write(HttpMethod.Patch, "Contact", contact, new() { ["Name"] = "Should be rejected" })).Should().BeFalse();
        (await Read("Contact", contact))["Name"].GetValue<string>().Should().Be(originalName);
        await SetDomains("");
        (await Write(HttpMethod.Patch, "Contact", contact, new() { ["Name"] = "Allowed after policy removal" })).Should().BeTrue();
    }

    [Test]
    public async Task DataServiceReturnsActionableValidationMessage() {
        Guid id = Guid.NewGuid();
        _records.Add(("Contact", id));
        object Value(object value, int type) => new { expressionType = 2, parameter = new { dataValueType = type, value } };
        using var response = await _client.PostAsJsonAsync("DataService/json/SyncReply/InsertQuery", new {
            rootSchemaName = "Contact", operationType = 1,
            columnValues = new { items = new Dictionary<string, object> {
                ["Id"] = Value(id, 0), ["Name"] = Value("Velux E2E DataService", 1), ["Email"] = Value("person@gmail.com", 1)
            } }
        });
        string body = await response.Content.ReadAsStringAsync();
        AllureApi.AddAttachment("DataService validation response", "application/json", System.Text.Encoding.UTF8.GetBytes(body));
        body.Should().Contain("prohibited").And.Contain("gmail.com");
        (await Read("Contact", id)).Should().BeNull();
    }
}
