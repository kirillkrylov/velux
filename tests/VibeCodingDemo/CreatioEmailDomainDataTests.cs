using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Terrasoft.Configuration.Tests;
using VibeCodingDemoApp.EmailDomains;

namespace VibeCodingDemo.Tests {
    [TestFixture]
    [MockSettings(RequireMock.DBEngine)]
    public class CreatioEmailDomainDataTests : BaseComposableAppTestFixture {
        [SetUp]
        public void ArrangeSchemas() {
            MockEntitySchemaWithColumns(ContactEmailDomainValidator.DomainSchemaName, new Dictionary<string, DataValueType> { { "Name", DataValueType.ShortText } });
            MockEntitySchemaWithColumns("Contact", new Dictionary<string, DataValueType> { { "Email", DataValueType.EmailText } });
            MockEntitySchemaWithColumns("CommunicationType", new Dictionary<string, DataValueType> { { "Name", DataValueType.ShortText } });
            MockEntitySchemaWithColumns("ContactCommunication", new Dictionary<string, DataValueType> { { "Number", DataValueType.ShortText } },
                new Dictionary<string, string> { { "Contact", "Contact" }, { "CommunicationType", "CommunicationType" } });
        }

        [Test]
        public void ReadsAllDomainLookupRows() {
            SetUpTestData(ContactEmailDomainValidator.DomainSchemaName, data => { },
                new Dictionary<string, object> { { "Id", Guid.NewGuid() }, { "Name", "gmail.com" } },
                new Dictionary<string, object> { { "Id", Guid.NewGuid() }, { "Name", "yahoo.com" } });
            new CreatioEmailDomainData(UserConnection).ReadDomains().Should().Equal("gmail.com", "yahoo.com");
        }

        [Test]
        public void ReadsPersistedContactEmail() {
            Guid id = Guid.NewGuid();
            SetUpTestData("Contact", data => data.Has(id), new Dictionary<string, object> { { "Id", id }, { "Email", "person@example.org" } });
            new CreatioEmailDomainData(UserConnection).ReadContactEmail(id).Should().Be("person@example.org");
        }

        [Test]
        public void ReadsCommunicationValueAndType() {
            Guid id = Guid.NewGuid();
            SetUpTestData("ContactCommunication", data => data.Has(id), new Dictionary<string, object> {
                { "Id", id }, { "Number", "person@gmail.com" }, { "CommunicationTypeId", ContactEmailDomainValidator.EmailTypeId }
            });
            CommunicationEmail email = new CreatioEmailDomainData(UserConnection).ReadCommunication(id);
            email.Number.Should().Be("person@gmail.com");
            email.TypeId.Should().Be(ContactEmailDomainValidator.EmailTypeId);
        }

        [Test]
        public void ReadsAllMatchingCommunicationEmails() {
            Guid contact = Guid.NewGuid();
            SetUpTestData("ContactCommunication", data => data.Has(contact).Has(ContactEmailDomainValidator.EmailTypeId),
                new Dictionary<string, object> { { "Id", Guid.NewGuid() }, { "Number", "one@example.org" } },
                new Dictionary<string, object> { { "Id", Guid.NewGuid() }, { "Number", "two@gmail.com" } });
            new CreatioEmailDomainData(UserConnection).ReadCommunicationEmails(contact)
                .Should().Equal("one@example.org", "two@gmail.com");
        }
    }
}
