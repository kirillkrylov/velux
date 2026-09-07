using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Terrasoft.Core.Entities;
using VibeCodingDemoApp.EmailDomains;
using VibeCodingDemoApp.EntryPoints.EntityEventListeners;

namespace VibeCodingDemo.Tests {
    [TestFixture]
    public class EmailDomainPolicyTests {
        [TestCase("person@gmail.com", "gmail.com")]
        [TestCase(" PERSON@GMAIL.COM ", "gmail.com")]
        [TestCase("person+tag@gmail.com", "gmail.com")]
        [TestCase("person@yahoo.com", "yahoo.com")]
        [TestCase("person@gmail.com.", "gmail.com")]
        [TestCase("person@sub.gmail.com", null)]
        [TestCase("person@notgmail.com", null)]
        [TestCase("person@gmail.com.example.org", null)]
        [TestCase("gmail.com@example.org", null)]
        [TestCase("person@example.org", null)]
        [TestCase("gmail.com", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        [TestCase("person@", null)]
        public void MatchesOnlyTheNormalizedDomain(string email, string expected) {
            new EmailDomainPolicy(" GMAIL.COM ; yahoo.com, gmail.com\r\n").GetBlockedDomain(email).Should().Be(expected);
        }

        [TestCase(null)] [TestCase("")] [TestCase(" ; ,\r\n ")]
        public void EmptyListAllowsEveryDomain(string list) {
            var policy = new EmailDomainPolicy(list);
            policy.IsEmpty.Should().BeTrue();
            policy.GetBlockedDomain("person@gmail.com").Should().BeNull();
        }

        [TestCase("bücher.de", "person@xn--bcher-kva.de")]
        [TestCase("xn--bcher-kva.de", "person@bücher.de")]
        [TestCase("gmail.com.", "person@gmail.com")]
        [TestCase("bad domain", "person@bad domain")]
        public void NormalizesBothSides(string list, string email) {
            new EmailDomainPolicy(list).GetBlockedDomain(email).Should().NotBeNull();
        }
    }

    [TestFixture]
    public class ContactEmailDomainValidatorTests : BaseComposableAppTestFixture {
        private IEmailDomainData _data;
        private ContactEmailDomainValidator _validator;

        [SetUp]
        public void ArrangePolicy() {
            MockEntitySchemaWithColumns("Contact", new Dictionary<string, DataValueType> {
                { "Email", DataValueType.EmailText }, { "Name", DataValueType.ShortText }
            });
            MockEntitySchemaWithColumns("ContactCommunication", new Dictionary<string, DataValueType> {
                { "Number", DataValueType.ShortText }, { "CommunicationTypeId", DataValueType.Guid }
            });
            _data = Substitute.For<IEmailDomainData>();
            _data.ReadDomains().Returns("gmail.com; yahoo.com");
            _data.ReadCommunicationEmails(Arg.Any<Guid>()).Returns(Array.Empty<string>());
            _data.ReadCommunication(Arg.Any<Guid>()).Returns(new CommunicationEmail { TypeId = ContactEmailDomainValidator.EmailTypeId, Number = "old@gmail.com" });
            _validator = new ContactEmailDomainValidator(_data);
        }

        private Entity Contact(string email) => CreateEntity("Contact", new Dictionary<string, object> {
            { "Id", Guid.NewGuid() }, { "Email", email }
        });

        [TestCase("user@gmail.com", true)] [TestCase("user@example.org", false)] [TestCase("", false)]
        public void ChecksPrimaryEmail(string email, bool rejected) {
            (_validator.Validate(Contact(email)) != null).Should().Be(rejected);
        }

        [Test]
        public void ChecksEveryExistingEmailCommunication() {
            _data.ReadCommunicationEmails(Arg.Any<Guid>()).Returns(new[] { "good@example.org", "bad@yahoo.com" });
            _validator.Validate(Contact("good@example.org")).Should().Contain("yahoo.com");
        }

        [Test]
        public void LoadsPrimaryEmailForPartialContactUpdates() {
            Entity contact = CreateEntity("Contact", new Dictionary<string, object> { { "Id", Guid.NewGuid() }, { "Name", "Updated" } });
            _data.ReadContactEmail(contact.PrimaryColumnValue).Returns("old@gmail.com");
            _validator.Validate(contact).Should().Contain("gmail.com");
        }

        [Test]
        public void ExplicitlyClearedEmailDoesNotReloadStoredValue() {
            _data.ReadContactEmail(Arg.Any<Guid>()).Returns("old@gmail.com");
            _validator.Validate(Contact("")).Should().BeNull();
            _data.DidNotReceive().ReadContactEmail(Arg.Any<Guid>());
        }

        [Test]
        public void EmptyPolicyDoesNotQueryContactData() {
            _data.ReadDomains().Returns("");
            _validator.Validate(Contact("bad@gmail.com")).Should().BeNull();
            _data.DidNotReceive().ReadCommunicationEmails(Arg.Any<Guid>());
        }

        [TestCase(true, "bad@gmail.com", true)]
        [TestCase(true, "good@example.org", false)]
        [TestCase(true, "", false)]
        [TestCase(false, "bad@gmail.com", false)]
        public void ChecksOnlyEmailCommunicationType(bool emailType, string number, bool rejected) {
            var entity = CreateEntity("ContactCommunication", new Dictionary<string, object> {
                { "Id", Guid.NewGuid() }, { "Number", number },
                { "CommunicationTypeId", emailType ? ContactEmailDomainValidator.EmailTypeId : Guid.NewGuid() }
            });
            (_validator.Validate(entity) != null).Should().Be(rejected);
            _data.DidNotReceive().ReadCommunication(Arg.Any<Guid>());
        }

        [TestCase("Number", "bad@yahoo.com")]
        [TestCase("CommunicationTypeId", null)]
        [TestCase("Id", null)]
        public void PartialCommunicationUpdatesUseStoredMissingFields(string field, string number) {
            var values = new Dictionary<string, object> { { "Id", Guid.NewGuid() } };
            if (field == "Number") { values[field] = number; }
            if (field == "CommunicationTypeId") { values[field] = ContactEmailDomainValidator.EmailTypeId; }
            _validator.Validate(CreateEntity("ContactCommunication", values)).Should().Contain("prohibited");
        }

        private static bool RaiseValidation(Entity entity) { entity.ValidationMessages.Clear(); typeof(Entity).GetMethod("OnValidating", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(entity, new object[] { new EntityValidationEventArgs(entity.ValidationMessages) }); return !entity.ValidationMessages.HasErrors(); }
        private sealed class TestListener : EmailDomainEntityEventListener {
            public IEmailDomainData Data { get; set; }
            protected override IEmailDomainData CreateData(Entity entity) => Data;
        }

        [TestCase("bad@gmail.com", true)] [TestCase("allowed@example.org", false)]
        public void SavingWithoutRequiredFieldValidationStillEnforcesPolicy(string email, bool cancelled) {
            Entity entity = Contact(email);
            var args = new EntityBeforeEventArgs();
            new TestListener { Data = _data }.OnSaving(entity, args);
            args.IsCanceled.Should().Be(cancelled);
        }

        [TestCase("bad@gmail.com", 1)] [TestCase("good@example.org", 0)]
        public void ListenerAddsValidationMessageAndSupportsRepeatedSaves(string email, int count) {
            Entity entity = Contact(email);
            var listener = new TestListener { Data = _data };
            listener.OnSaving(entity, new EntityBeforeEventArgs());
            listener.OnSaving(entity, new EntityBeforeEventArgs());
            RaiseValidation(entity);
            entity.ValidationMessages.Count.Should().Be(count);
            if (count > 0) {
                entity.ValidationMessages[0].Column.Name.Should().Be("Email");
                entity.ValidationMessages[0].Text.Should().Contain("gmail.com");
                entity.SetColumnValue("Email", "fixed@example.org");
                RaiseValidation(entity).Should().BeTrue();
            }
        }

        [Test]
        public void CommunicationMessageTargetsNumber() {
            var entity = CreateEntity("ContactCommunication", new Dictionary<string, object> {
                { "Id", Guid.NewGuid() }, { "Number", "bad@gmail.com" }, { "CommunicationTypeId", ContactEmailDomainValidator.EmailTypeId }
            });
            new TestListener { Data = _data }.OnSaving(entity, new EntityBeforeEventArgs());
            RaiseValidation(entity).Should().BeFalse();
            entity.ValidationMessages[0].Column.Name.Should().Be("Number");
        }
    }
}
