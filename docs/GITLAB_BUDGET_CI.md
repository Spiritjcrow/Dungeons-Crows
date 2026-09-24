# GitLab budget Unity validation

GitLab is the secondary CI lane for Dungeons & Crows while GitHub-hosted jobs are failing before runner execution.

## Cost control

The GitLab Unity job is deliberately manual. It does not run for every push or merge request. This preserves free compute minutes while the project is still in rapid iteration.

The job performs only the current hard gate:

1. Start a GameCI Unity Editor container for Unity 6000.0.65f1.
2. Activate the configured Unity license.
3. Open and compile the Unity project.
4. Run EditMode tests, including OnlineProtocolTests and Alpha4PresentationTests.
5. Preserve XML results and Editor logs.
6. Return the Unity license.

It does not build Windows, Android, WebGL, or other player targets yet. Those are intentionally postponed until EditMode validation passes.

## Required GitLab CI/CD variables

Set these as masked variables in the GitLab project:

- UNITY_EMAIL
- UNITY_PASSWORD
- UNITY_SERIAL

Do not commit these values to the repository.

## Project layout

The Unity project remains under UnityProject/. The GitLab job uses that folder directly and does not create a second game implementation.
