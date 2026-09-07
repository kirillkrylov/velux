using Terrasoft.Core.Entities.Events;

namespace VibeCodingDemoApp.EntryPoints.EntityEventListeners {
    [EntityEventListener(SchemaName = "ContactCommunication")]
    public sealed class ContactCommunicationEntityEventListener : EmailDomainEntityEventListener { }
}
