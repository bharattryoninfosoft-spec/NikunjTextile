# NikunjTextile — Mandatory AI Coding Rules

These rules apply to every AI-assisted change in this repository. Read this file before inspecting or editing code. If a user request conflicts with these safety rules, stop and ask for clarification.

## 1. Mission and scope

- Preserve the existing NikunjTextile application, architecture, framework, business workflows, UI, reports, APIs, database schema, and integrations unless the user explicitly authorizes a specific change.
- This is a legacy ASP.NET ASMX application targeting .NET Framework 4.7.2. Do not migrate frameworks, upgrade packages, replace libraries, restructure the solution, or introduce new architecture as part of an integrity-fix task.
- Work only on the exact WebMethod, helper, stored procedure, table, or issue authorized for the current task.
- Never perform broad cleanup, formatting, renaming, refactoring, optimization, or unrelated bug fixes in the same change.
- Do not edit generated files, designer files, RDLC/report layouts, configuration, deployment files, or unrelated modules unless the task explicitly requires it.

## 2. Protect the current workspace first

Before any edit:
1. Inspect the current Git branch, `git status`, and existing diff.
2. Identify and preserve all user changes, untracked files, and in-progress Antigravity work. Never overwrite, reset, clean, stash, checkout, rebase, or discard them without explicit approval.
3. If another agent/task is actively editing, do not interrupt it. Wait until it finishes or ask the user how to proceed.
4. Confirm a recoverable checkpoint exists. Prefer a dedicated audit/fix branch and a commit of only the authorized files. If the working tree is dirty, do not create a broad commit; ask the user to preserve or resolve the existing work first.
5. Never force-push, rewrite shared history, or change Git remotes/credentials.

Before database work:
- Confirm the target database/environment. Never assume local, test, or production.
- Require a verified full backup and a tested restore path before any schema/data-changing operation.
- Use read-only inspection first. Do not run destructive SQL, migrations, bulk updates, deletes, truncation, or constraint changes against production without explicit approval.
- Do not put credentials, connection strings, tokens, or personal data in source control, logs, prompts, or reports.

## 3. One-change-at-a-time workflow

For every WebMethod or database object:
1. Record its exact name, file, line range, callers/dependencies, tables, stored procedures, and current behavior.
2. Read the complete method and relevant helper/query/schema definitions. Do not infer behavior from the method name alone.
3. Write a short finding and proposed minimal change before editing.
4. Change only the authorized method/object and the minimum necessary shared helper code.
5. Review the complete diff. Confirm no unrelated lines/files changed.
6. Build using the solution's existing target framework and configuration.
7. Run focused tests for the changed behavior, including success and failure/rollback cases where possible.
8. Run relevant regression checks for callers and dependent workflows.
9. Update the audit tracker with evidence: reviewed, changed, build result, tests run, remaining risks, and commit hash.
10. Commit the verified unit before starting the next unit, if the user-approved Git workflow permits it.

If any step fails, stop. Do not continue to the next WebMethod. Report the exact failure and wait for direction.

## 4. Database integrity and dependency rules

- Never delete a parent/master record while dependent child, transaction, history, ledger, stock, invoice, or report-referenced records exist.
- Before a delete, identify all direct and indirect dependencies using actual foreign keys, application queries, stored procedures, and business rules. Do not rely on foreign keys alone.
- If dependencies exist, block the delete and return a clear, useful message identifying the dependent record type/count. Do not silently cascade-delete business data.
- Do not add `ON DELETE CASCADE` or `ON UPDATE CASCADE` without explicit approval and a documented business case.
- Prefer soft-delete/deactivation only if the existing application has a compatible status field and all relevant workflows respect it. Do not invent a soft-delete scheme without approval.
- Before adding a foreign key, primary key, unique constraint, or check constraint, inspect existing data for duplicates, nulls, and orphan rows. Report cleanup requirements; never silently delete or rewrite existing data to make a constraint pass.
- Do not change column types, lengths, nullability, defaults, identity settings, or keys without a documented impact analysis and approval.
- Treat database scripts as proposed changes until executed and verified in the intended environment. A script review is not proof of live database integrity.

## 5. ID and key immutability

- Primary keys and identity IDs are immutable after record creation.
- UPDATE methods must never assign, replace, regenerate, or accept a client-supplied change to the existing primary key/identity value.
- Use the database record's existing key in the WHERE clause and update only permitted non-key fields.
- Do not delete and reinsert a record as a substitute for UPDATE.
- Validate that exactly the intended row was updated or deleted. If affected rows are zero or unexpected, treat it as a failure and do not log success.
- For parent-child records, preserve existing relationship IDs unless an explicitly approved business operation changes the relationship.

## 6. Transaction and audit logging rules

- Every business INSERT/UPDATE/DELETE that requires an audit record must execute the business change and its success AuditLog insert in the same SQL connection and transaction.
- The order must be: begin transaction -> read old values in that transaction -> perform validated change -> verify affected rows -> insert success audit record using the same connection and transaction -> commit.
- If the business query or required success audit insert fails, roll back the whole transaction. Never commit business data without its required audit record.
- A transaction-bound audit failure must be thrown back to the caller. It must never silently fall back to a separate connection or independent audit insert.
- Do not swallow exceptions in strict transactional audit paths. Preserve the original exception when handling rollback.
- Fetch old values before UPDATE/DELETE, using the actual primary key and the same transaction. Never guess a key from a table-name pattern or use the first column as a fallback.
- Technical error logging after rollback may use a separate connection, but it must not create a false success audit record or mask the original exception.
- Avoid duplicate ErrorLog records. Check whether the caller/helper already writes the same exception.
- Login/validation telemetry may have a different best-effort policy only if that policy is explicitly documented and does not weaken business-change audit guarantees.
- Never move `Commit()` before the required success AuditLog insert.

## 7. SQL and input safety

- Use parameterized SQL for all values. Do not concatenate user input into SQL statements.
- SQL identifiers (table/column names) cannot be parameterized; allowlist them and escape identifiers safely. Never accept arbitrary user-supplied identifiers.
- Specify appropriate SQL parameter types and lengths. Avoid implicit conversions and unsafe string-to-number/date conversions.
- Validate required values, ranges, IDs, ownership/permissions, and related-record existence on the server.
- Do not trust client-side validation or hidden form fields.
- Preserve existing authorization checks and add no bypasses.
- Use explicit column lists in INSERT statements; do not rely on table column order.

## 8. Error handling and external side effects

- Use try/catch/finally carefully. Rollback must be guarded so a rollback exception does not hide the original failure.
- Do not return success when the database operation failed, affected no rows unexpectedly, or required audit logging failed.
- Do not expose SQL text, stack traces, credentials, or internal server details to end users.
- File deletion, image replacement, email, WhatsApp, API calls, and other external side effects cannot be rolled back by a SQL transaction. Do not perform irreversible side effects before the database commit unless a documented compensation strategy exists.
- If a file operation fails after commit, report and log the partial failure accurately; do not pretend the whole operation rolled back.

## 9. Compatibility / no-break policy

- Preserve public WebMethod names, signatures, parameter names/types, response shapes, JSON/XML contracts, stored procedure parameter contracts, and existing callers unless the user explicitly approves a breaking change.
- Preserve existing business calculations, tax/rounding logic, stock/accounting rules, report layouts, and UI behavior.
- Do not remove controls, scripts, CSS, NuGet packages, DLL references, or configuration entries unless explicitly requested.
- Do not change connection strings, database names, ports, hosting/deployment settings, authentication, or permissions as part of unrelated fixes.
- Do not claim a feature works unless it has been tested in the relevant environment.

## 10. Required verification

For each change, perform and record the checks that are available:
- Compile/build the existing solution with its current target framework.
- Inspect the final diff and changed-file list.
- Test normal success, invalid input, missing record, duplicate/conflict, dependency-blocked delete, and database/audit failure paths as applicable.
- Verify rollback leaves no partial business changes and no false success audit record.
- Verify the primary key remains unchanged after UPDATE.
- Verify unrelated existing workflows and report/API contracts remain unchanged.
- For SQL changes, run read-only pre-check queries first and validate row counts and constraints after an approved execution.
- If a test cannot be run (for example, no test database, no credentials, or unavailable device/API), mark it NOT TESTED. Never mark it passed based on code inspection alone.

## 11. Forbidden actions without explicit user approval

- Whole-project rewrite, migration, mass refactor, mass formatting, or broad search-and-replace.
- Editing multiple unrelated WebMethods in one batch.
- Deleting or renaming files, tables, columns, methods, or stored procedures.
- Changing primary keys, identity values, foreign keys, cascade rules, or production data.
- Running destructive SQL or applying schema changes to production.
- Installing/upgrading packages or changing framework/runtime versions.
- Changing Git history, remotes, branches with uncommitted work, or deployment configuration.
- Disabling validation, authorization, audit logging, constraints, or error handling to make a test pass.

## 12. Required report after each unit

Provide:
- Object/method name and exact files changed.
- What was wrong and the minimal fix made.
- Database tables/relationships affected.
- Build result and exact tests executed.
- Whether transaction rollback, audit atomicity, delete dependency protection, and ID immutability were verified.
- Remaining risks or NOT TESTED items.
- Git commit/checkpoint identifier, if created.
- Tracker row updated.

Never report “all bugs fixed”, “100% safe”, or “zero future bugs”. State only what the evidence proves.

## 13. First task sequence for the integrity audit

Follow this order unless the user changes it:
1. Inspect and preserve the current workspace; establish a safe checkpoint without disturbing existing work.
2. Review `AuditLogHelper` and design a strict transaction-bound audit path. Do not change all callers globally until compatibility is assessed.
3. Fix and test one known transaction-order issue (including `EditGodownMaster`) as a pilot.
4. Audit database relationships and existing data read-only; report orphan/duplicate/missing-key candidates before proposing schema changes.
5. Process WebMethods individually, updating the tracker only after verification.
6. Review stored procedures and run regression tests.
7. Provide a final evidence-based report and unresolved-risk list.

**Stop rule:** If the current workspace is already being modified, contains unexplained changes, cannot be built, lacks a verified backup for a database change, or the requested change may affect unrelated functionality, do not guess or proceed. Preserve the state and ask the user.
