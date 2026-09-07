using System;
using System.Collections.Generic;
using Terrasoft.Core.Entities;

namespace VibeCodingDemoApp.EmailDomains {
    public interface IEmailDomainData {
        string ReadDomains();
        string ReadContactEmail(Guid contactId);
        IEnumerable<string> ReadCommunicationEmails(Guid contactId);
        CommunicationEmail ReadCommunication(Guid communicationId);
    }

    public sealed class CommunicationEmail {
        public string Number { get; set; }
        public Guid TypeId { get; set; }
    }

    public sealed class ContactEmailDomainValidator {
        public const string SettingCode = "UsrProhibitedEmailDomains";
        // Creatio CommunicationTypeConsts.EmailId (CrtBaseConsts).
        public static readonly Guid EmailTypeId = new Guid("ee1c85c3-cfcb-df11-9b2a-001d60e938c6");
        private readonly IEmailDomainData _data;
        public ContactEmailDomainValidator(IEmailDomainData data) { _data = data; }

        public string Validate(Entity entity) {
            var policy = new EmailDomainPolicy(_data.ReadDomains());
            if (policy.IsEmpty) { return null; }
            if (entity.Schema.Name == "Contact") {
                string email = entity.GetIsColumnValueLoaded("Email")
                    ? entity.GetTypedColumnValue<string>("Email") : _data.ReadContactEmail(entity.PrimaryColumnValue);
                string blocked = policy.GetBlockedDomain(email);
                if (blocked != null) { return Message(blocked); }
                foreach (string communication in _data.ReadCommunicationEmails(entity.PrimaryColumnValue)) {
                    blocked = policy.GetBlockedDomain(communication);
                    if (blocked != null) { return Message(blocked); }
                }
                return null;
            }
            CommunicationEmail stored = null;
            if (!entity.GetIsColumnValueLoaded("Number") || !entity.GetIsColumnValueLoaded("CommunicationTypeId")) {
                stored = _data.ReadCommunication(entity.PrimaryColumnValue);
            }
            Guid type = entity.GetIsColumnValueLoaded("CommunicationTypeId")
                ? entity.GetTypedColumnValue<Guid>("CommunicationTypeId") : stored.TypeId;
            if (type != EmailTypeId) { return null; }
            string number = entity.GetIsColumnValueLoaded("Number")
                ? entity.GetTypedColumnValue<string>("Number") : stored.Number;
            string prohibited = policy.GetBlockedDomain(number);
            return prohibited == null ? null : Message(prohibited);
        }

        private static string Message(string domain) =>
            "Email domain '" + domain + "' is prohibited for Contacts. Use an email address with an allowed domain.";
    }
}
