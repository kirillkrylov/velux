# Contributing

All contributions follow the same workflow: **issue → linked branch → pull request**.
Track the issue through **Investigating → Fixing → QA**.

## Before starting work

1. Create a GitHub issue or use an existing issue that describes the intended outcome and acceptance criteria. Every new piece of work requires an issue, including documentation and maintenance changes.
2. Assign the issue to yourself. Coordinate with the current assignee before working on an issue owned by someone else.
3. Create a dedicated branch from the current default branch and link it to the issue through GitHub's **Development** section. Use a predictable name such as `<github-login>/issue-<number>`.
4. Set the issue's stage to **Investigating** before beginning work.

Every issue being worked on must have a linked branch. A branch name or an issue URL in a comment does not replace the Development link. Keep the branch until the work is merged or explicitly abandoned.

## Issue stages

Use the repository's configured issue field, project field, or labels to record one current stage. Keep it updated as work progresses.

| Stage | Meaning | Exit criteria |
| --- | --- | --- |
| **Investigating** | Understand the request, reproduce the problem where applicable, and determine the approach. | The scope, acceptance criteria, and proposed solution are clear. |
| **Fixing** | Implement the agreed change, including relevant tests and documentation. | Implementation is complete and ready for validation. |
| **QA** | Validate the acceptance criteria, run required checks, and complete PR review. | Required checks pass and blocking review feedback is resolved. |

If QA identifies changes that are needed, return the issue to **Fixing**, then move it back to **QA** for validation. Record blockers on the issue and keep its stage accurate.

## Pull requests

- All contributions must be merged through a pull request. Do not commit or push contributions directly to the default branch or any release branch.
- Open the PR from the issue's linked branch. Link the PR to the issue; use `Closes #123` or `Fixes #123` when it fully resolves that issue.
- Keep the PR focused on the issue's scope. Update the issue before materially changing the scope.
- Explain what changed, why, and how it was validated. Identify any remaining limitations.
- Keep incomplete work in a draft PR. Request review when implementation is complete and the issue enters **QA**.
- Merge only after required checks and repository review requirements are satisfied and all blocking feedback is resolved.

## Completion

After the PR is merged, verify that the issue's acceptance criteria are satisfied and close the issue. If multiple PRs are required, keep the issue open until all required work is merged and verified.

Delete the completed branch only after merge. Issue closure records completion; no additional stage after **QA** is required.
