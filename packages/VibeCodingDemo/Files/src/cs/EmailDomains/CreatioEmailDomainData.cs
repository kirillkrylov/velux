using System;
using System.Collections.Generic;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace VibeCodingDemoApp.EmailDomains {
    public sealed class CreatioEmailDomainData : IEmailDomainData {
        private readonly UserConnection _connection;
        public CreatioEmailDomainData(UserConnection connection) { _connection = connection; }
        public string ReadDomains() => Terrasoft.Core.Configuration.SysSettings.GetValue(
            _connection, ContactEmailDomainValidator.SettingCode, "");

        // Deliberately bypass record filtering for integrity checks: hidden email rows must
        // not allow a Contact to evade the policy. No row contents are returned to callers.
        private EntitySchemaQuery Query(string schema) => new EntitySchemaQuery(_connection.EntitySchemaManager, schema) {
            UseAdminRights = false
        };

        public string ReadContactEmail(Guid contactId) {
            var query = Query("Contact");
            query.AddColumn("Email");
            return query.GetEntity(_connection, contactId)?.GetTypedColumnValue<string>("Email");
        }

        public IEnumerable<string> ReadCommunicationEmails(Guid contactId) {
            var query = Query("ContactCommunication");
            query.AddColumn("Number");
            query.Filters.Add(query.CreateFilterWithParameters(FilterComparisonType.Equal, "Contact", contactId));
            query.Filters.Add(query.CreateFilterWithParameters(FilterComparisonType.Equal, "CommunicationType", ContactEmailDomainValidator.EmailTypeId));
            foreach (Entity entity in query.GetEntityCollection(_connection)) {
                yield return entity.GetTypedColumnValue<string>("Number");
            }
        }

        public CommunicationEmail ReadCommunication(Guid communicationId) {
            var query = Query("ContactCommunication");
            query.AddColumn("Number");
            query.AddColumn("CommunicationType");
            Entity entity = query.GetEntity(_connection, communicationId);
            return new CommunicationEmail {
                Number = entity?.GetTypedColumnValue<string>("Number"),
                TypeId = entity?.GetTypedColumnValue<Guid>("CommunicationTypeId") ?? Guid.Empty
            };
        }
    }
}
