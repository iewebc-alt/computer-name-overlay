# Publication and SignPath setup

## Before applying

1. Publish this directory alone as a public GitHub repository, preferably `computer-name-overlay`. Do not publish the parent workspace, domain deployment data or wallpaper images.
2. Complete MAINTAINERS.md and the URLs in the application draft. Enable MFA for the responsible accounts. Enable private vulnerability reporting and protect release branches/tags as appropriate.
3. Run CI, review the app manually, and publish the initial unsigned release from tag `v0.1.0`. Keep signing disabled. Confirm its public Releases page contains a downloadable EXE package and a Code signing policy link.
4. Review the Foundation conditions and submit the application yourself. This project has no claimed existing reputation; acceptance is not assured.

The code and draft are preparation, not evidence that repository settings, MFA, release approval or SignPath acceptance have already been configured.

## After approval

Use the SignPath GitHub trusted-build integration and connect only this repository. In SignPath, configure an artifact from the CI `unsigned-binary` ZIP. It must select only `ComputerNameOverlay.exe`, enforce ProductName `Computer Name Overlay` and the intended release's ProductVersion and FileVersion, and sign that EXE. Configure a production signing policy requiring a named maintainer's manual approval.

The upload action wraps the EXE in a ZIP; the SignPath artifact configuration therefore needs a ZIP root. An initial-release template is included in `signpath/artifact-configuration.xml`, based on the [official examples](https://docs.signpath.io/artifact-configuration/examples). Validate it in the approved SignPath project against the actual CI artifact before use; update its version restrictions for each release.

Create the GitHub **release** environment; configure reviewers/release-tag restrictions appropriate to the team. Add these repository variables:

| Variable | Value |
|---|---|
| SIGNPATH_ENABLED | `true` only after approval and successful setup |
| SIGNPATH_ORGANIZATION_ID | Provided by SignPath |
| SIGNPATH_PROJECT_SLUG | The approved project slug |
| SIGNPATH_SIGNING_POLICY_SLUG | The manual-approval production policy |
| SIGNPATH_ARTIFACT_CONFIGURATION_SLUG | The validated ZIP/EXE configuration |

Add `SIGNPATH_API_TOKEN` as an environment secret in **release**, with only the needed submission permission. Do not commit tokens or paste them into issues. A token cannot approve its own production release; manual approval remains in SignPath. Use test-signing separately before enabling production.

When SIGNPATH_ENABLED is true, a failed signature request or invalid signature fails the release. When false/unset, the release is explicitly unsigned. The workflow does not submit signing requests from pull requests. Its action dependencies are pinned to upstream commits. Update pins through reviewed changes.

After the first approved production signature, update the status and attribution in README and CODE_SIGNING.md. Keep a clear distinction between legacy unsigned releases and production-signed releases.

## Versioning

Initial version is 0.1.0. Update src/AssemblyInfo.cs, the expected metadata in build.ps1/package.ps1, and release notes together. Push a matching `vX.Y.Z` tag. For release retries, inspect existing assets and create a new version rather than silently overwriting published assets; `gh release create` deliberately fails if the release already exists.

## References

- https://signpath.org/apply.html
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github
- https://docs.signpath.io/artifact-configuration/
