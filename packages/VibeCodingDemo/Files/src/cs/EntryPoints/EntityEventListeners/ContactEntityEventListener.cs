using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;
using VibeCodingDemoApp.EmailDomains;

namespace VibeCodingDemoApp.EntryPoints.EntityEventListeners {
    public abstract class EmailDomainEntityEventListener : BaseEntityEventListener {
        protected virtual IEmailDomainData CreateData(Entity entity) => new CreatioEmailDomainData(entity.UserConnection);

        public override void OnSaving(object sender, EntityBeforeEventArgs e) {
            base.OnSaving(sender, e);
            var entity = (Entity)sender;
            // Repeated saves of the same Entity must not accumulate subscriptions.
            entity.Validating -= ValidateEmail;
            entity.Validating += ValidateEmail;
            // Save(false) disables required-field validation, not this integrity policy.
            if (!e.IsValidationEnabled) {
                string error = new ContactEmailDomainValidator(CreateData(entity)).Validate(entity);
                if (error != null) {
                    AddMessage(entity, error);
                    e.IsCanceled = true;
                }
            }
        }

        private void ValidateEmail(object sender, EntityValidationEventArgs e) {
            var entity = (Entity)sender;
            string error = new ContactEmailDomainValidator(CreateData(entity)).Validate(entity);
            if (error != null) {
                AddMessage(entity, error);
            }
        }

        private static void AddMessage(Entity entity, string error) {
            entity.ValidationMessages.Add(new EntityValidationMessage {
                Column = entity.Schema.Columns.GetByName(entity.Schema.Name == "Contact" ? "Email" : "Number"),
                Text = error
            });
        }
    }

    [EntityEventListener(SchemaName = "Contact")]
    public sealed class ContactEntityEventListener : EmailDomainEntityEventListener { }
}
