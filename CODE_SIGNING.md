# Code signing policy

## Current status

Signing is not enabled. The project is preparing an application to SignPath Foundation; it has not been accepted and its initial build is unsigned. Do not interpret this page as an endorsement.

After approval and the first production-signed release, publish this attribution on the home and release pages:

> Free code signing provided by SignPath.io, certificate by SignPath Foundation

## Roles

The responsible public GitHub identity is [iewebc-alt](https://github.com/iewebc-alt), listed in [MAINTAINERS.md](MAINTAINERS.md). Production-signing access remains inactive until SignPath approval and account setup.

Authors maintain this project's source and build scripts. Reviewers review outside contributions, including CI changes. Release approvers check the source revision, build result and intended release, then manually approve the production request in SignPath. Project maintainers with repository or signing access must use MFA.

## Release process

Only this project's own EXE, built from a reviewed version tag by the checked-in GitHub-hosted workflow, is eligible. No arbitrary uploaded binaries or external proprietary components are accepted. The workflow uploads its unsigned artifact before requesting signing, preserving the association with its GitHub build.

Production signing must require manual approval in SignPath. A GitHub environment approval is an additional gate, not a replacement. The workflow waits for the signed result and checks Authenticode before packaging it. Failed or unconfigured signing never silently produces a package labelled signed.

The artifact configuration must enforce ProductName `Computer Name Overlay`, the expected file name, and the release's product/file version. Release assets include a SHA-256 checksum. Unsigned releases remain explicitly labelled unsigned.

## Privacy

The application does not transfer data to networked systems. See [PRIVACY.md](PRIVACY.md) for local data and visible-hostname details.

Reference: [SignPath Foundation conditions](https://signpath.org/terms.html).
