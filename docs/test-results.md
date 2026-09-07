# Verified delivery — 7 September 2026

Target: the registered **Velux** Creatio environment, file-system mode enabled. The deployed package is linked to this workspace. Validation used the real authenticated OData and DataService endpoints.

| Check | Result |
| --- | --- |
| Solution build, `dev-n8` | Passed |
| NUnit unit tests | 44 passed, 0 failed |
| Feature line coverage | 99.11% |
| Feature branch coverage | 92.85% |
| Enforced minimum | 80% lines and branches |
| Live API E2E tests | 22 passed, 0 failed, 0 skipped |
| Test Contact cleanup | No `Velux E2E` Contacts or initial rejected probe remain |
| Restored lookup rows | `gmail.com`, `yahoo.com` (original IDs preserved) |

[Download/open the standalone Allure report](allure-report/index.html). When viewing this repository on GitHub, download the HTML using **Raw / Download raw file**, then open it locally. No report server is required. Request/response evidence contains only synthetic test records. [Cobertura coverage](coverage.cobertura.xml) and [E2E TRX](e2e.trx) are included alongside the report.

The E2E scenarios cover eight Contact creation cases; allowed/rejected/cleared primary-email updates; allowed/rejected email communication creation; communication number-only updates; changing a non-email communication to a blocked email; newly blocked primary and secondary legacy emails; immediate configuration changes; an actionable DataService validation response; immediate lookup add/edit/delete effects; and registration in the regular Lookups administration area. Every write scenario verifies stored state. Tests clean up their own IDs and restore the original lookup rows.

Coverage includes every new production type in `EmailDomains` and `EntryPoints.EntityEventListeners`, including the Creatio adapter and its lookup-row query. Starter-kit application composition/localization code and platform binaries are not included. Adapter tests use mocked database results with Contact and email-type filter predicates. Live E2E tests verify those queries against the actual runtime.

Limitations: browser click paths and a fresh installation into a second environment were not tested. `Save(false)` enforcement is covered by focused listener unit tests; live E2E tests use normal API saves. The unchanged scaffold produces NuGet warnings about System.Text.Json 8.0.0 and the legacy CreatioSDK dependency of ATF.Repository. Those dependency migrations are not part of the domain-policy change.
