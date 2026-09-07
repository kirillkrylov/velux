# Velux Contact email domain policy

Server-side validation for Contact primary email and email communication options. Uses the existing `VibeCodingDemo` Creatio package.

## Configure

Open **System designer → Lookups**, search for **Prohibited email domains**, and open its contents. Add one row per domain, entering the domain in **Name**:

| Name |
| --- |
| gmail.com |
| yahoo.com |

The lookup object is `UsrProhibitedEmailDomain`, inherited from Creatio's `BaseLookup`. Use the regular lookup editor to add, edit, or delete rows. Enter domains only, without `@`, wildcards, URLs, or email addresses. Matching ignores case, surrounding whitespace, and a terminal DNS dot; Unicode and punycode domains are equivalent. Matching is exact: blocking `gmail.com` does not block `sub.gmail.com` or `notgmail.com`. List subdomains explicitly when needed. An empty lookup permits every domain. Changes apply on the next save, without restart.

Velux contains `gmail.com` and `yahoo.com`, migrated from the previous system setting. The obsolete system setting has been removed and is no longer read. The package ships the lookup object and its registration in **Lookups**; domain rows remain environment-owned and are not overwritten by package updates. A fresh installation begins with an empty lookup.

## Behavior

- Contact creation and updates validate the current primary email and every persisted email communication, including an email hidden by record permissions. The validation response identifies the domain, without disclosing another communication's address.
- Creating or updating an email communication validates its current email address. Partial updates load any omitted fields from storage. Changing another communication type to email also validates the existing number.
- Phone and other non-email types remain unaffected. Email communications use Creatio's built-in Email communication type.
- A newly prohibited legacy address blocks subsequent Contact edits, including changes to unrelated fields. Correct or delete prohibited communication rows before saving the Contact; correcting only the primary address does not repair a separate communication row.
- Our code adds native `EntityValidationMessage` errors. DataService returns an actionable message. OData may return a generic HTTP error; rejected writes are verified absent/unchanged by the tests.
- Backend `Entity.Save(false)` is also cancelled for prohibited domains. Direct SQL writes or explicitly disabled entity events bypass the platform event pipeline and are outside this feature's enforcement boundary.
- This is domain policy, not a replacement for Creatio's email syntax validation. Existing records are not retroactively modified.

The implementation consists of a regular lookup, one domain matcher, one validator, one Creatio data adapter, and two schema-bound listeners. There are no background jobs, custom endpoints, or custom UI components.

## Build and deploy

Prerequisites: a licensed Creatio .NET 8 development environment, .NET 8 and 10 SDKs, PowerShell 7, and Clio. The existing Velux workspace is linked to its instance in file-system mode.

```powershell
dotnet build MainSolution.slnx -c dev-n8
pwsh -File tasks/test-unit.ps1
```

After changing C# in this linked workspace, build and restart **Velux** using Clio's `restart-by-environment-name` tool. Do not use `push-workspace` or `compile-creatio` in file-system mode. For a new installation, install the package including the `UsrProhibitedEmailDomain` schema and `Data/Lookup_UsrProhibitedEmailDomain`, then configure the domain list. For filesystem metadata changes use the current Clio `pkg-to-db` contract. Always inspect the target's FSM mode and current Clio deployment guidance first.

The public repository excludes proprietary reference assemblies, local environment configuration, and credentials. On a fresh checkout, download matching Creatio configuration into `.application` with Clio and restore test/package libraries from your licensed Clio installation and composable-app starter kit:

```powershell
pwsh -File tasks/restore-local-libraries.ps1 -ClioTemplates '<clio-content/tpl>' -PackageLibraries '<starter-package/Files/Libs>'
```

Clio's `tpl/UnitTestLibs` supplies the unit-test framework. The package library directory must contain `ATF.Repository.dll` and `ErrorOr.dll`. Do not publish these binaries or replace the generated fixture while restoring libraries.

## Tests and Allure

```powershell
pwsh -File tasks/test-unit.ps1
pwsh -File tasks/test-e2e.ps1 -EnvironmentName Velux
```

The unit runner enforces **at least 80% line and branch coverage** over all new feature production types: `EmailDomains.*` and `EntryPoints.EntityEventListeners.*`. Unrelated starter-kit code and Creatio assemblies are outside that denominator. Cobertura output is saved under `artifacts/unit`.

The live suite uses NUnit, FluentAssertions, and Allure.NUnit. It exercises authenticated OData and DataService against the real Creatio runtime, reads persisted outcomes, and removes its own records. This is API E2E coverage; browser interaction and visual presentation are not automated. Run against a dedicated test environment: tests temporarily change the shared domain lookup and restore its original rows (IDs, names, and descriptions) after each test. Do not run multiple copies concurrently.

Install Allure CLI 3 (`allure` on PATH). Each E2E run writes TRX, Allure results, and a generated report under `artifacts/e2e/<timestamp>`. Open that run's `allure-report/index.html`. The runner reads credentials from the named local Clio registration and passes them only through process environment variables. CI can instead supply `CREATIO_URL`, `CREATIO_IS_NETCORE=true`, and either `CREATIO_ACCESS_TOKEN` or `CREATIO_USERNAME`/`CREATIO_PASSWORD` to `dotnet test` directly.

See [verified results](docs/test-results.md) for the delivery evidence and limitations.
