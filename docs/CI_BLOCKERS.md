# CI Blockers

## GitHub Actions runner assignment

Observed on 2026-09-17 while validating the `dc-turn/1.0` bridge.

Two unrelated workflows failed before executing any step:

- `Protocol Contract` run `35274949550`
- `Selma Repo Health` run `35274949557`

Both jobs reported:

- `status: completed`
- `conclusion: failure`
- `steps: []`
- `runner_id: 0`
- no runner name or runner group

This means the failures occurred before workflow commands ran. The protocol checker itself was reproduced independently and returned success with matching version, entity type, action list, and limits.

Until GitHub Actions runner assignment is restored for the repository/account, CI results from these runs must not be interpreted as application/test failures.
