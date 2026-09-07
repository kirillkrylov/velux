using System;
using System.Collections.Generic;
using System.Globalization;

namespace VibeCodingDemoApp.EmailDomains {
    /// <summary>Exact, case-insensitive domain matching; never suffix matching.</summary>
    public sealed class EmailDomainPolicy {
        private readonly HashSet<string> _domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public EmailDomainPolicy(IEnumerable<string> configuredDomains) {
            foreach (string item in configuredDomains ?? Array.Empty<string>()) {
                string domain = Normalize(item);
                if (domain.Length > 0) { _domains.Add(domain); }
            }
        }

        public bool IsEmpty => _domains.Count == 0;

        public string GetBlockedDomain(string email) {
            if (string.IsNullOrWhiteSpace(email)) { return null; }
            string value = email.Trim();
            int at = value.LastIndexOf('@');
            if (at < 0) { return null; }
            string domain = Normalize(value.Substring(at + 1));
            return _domains.Contains(domain) ? domain : null;
        }

        private static string Normalize(string value) {
            string domain = (value ?? "").Trim().TrimEnd('.');
            // IDN and its ASCII representation identify the same DNS domain.
            try { return new IdnMapping().GetAscii(domain).ToLowerInvariant(); }
            catch (ArgumentException) { return domain.ToLowerInvariant(); }
        }
    }
}
