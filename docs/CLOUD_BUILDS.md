# Cloud builds and phone iteration

## Project identifiers (not secrets)

- Source: https://github.com/lukechn99/underpass
- Unity organization: `2476086268667`
- Unity Cloud project: `9f904f49-4516-40bf-a176-bffabb93064d`
- [Project dashboard](https://cloud.unity.com/organizations/2476086268667/projects/9f904f49-4516-40bf-a176-bffabb93064d)
- Editor pin: `6000.3.28f1`, revision `8f082d2fe476`, Unity 6.3 LTS.

The cloud service supplies its editor. Having an editor installed on the user's
Mac does not make it available to the managed development workspace. The editor
and license are not configured in that workspace; this document is setup, not a
claim of an operating build pipeline.

## First target: Web

In Unity Cloud, open DevOps → Build Automation:

1. Connect Git source `https://github.com/lukechn99/underpass.git`. Grant the build
   service source access using its supported GitHub/PAT or SSH setup. Keep any
   token in the service's credential store, not source or chat.
2. Select branch `main`. Project subfolder is empty: `Assets`, `Packages` and
   `ProjectSettings` are at the repository root.
3. Create a **WebGL** build configuration and enable editor version detection
   from `ProjectSettings/ProjectVersion.txt`. Confirm that exact pinned patch is
   offered by the service; if not, choose a supported 6.3 LTS patch and update
   the pin deliberately rather than silently building another version.
4. Use scene `Assets/Scenes/Trial.unity`. The pre-build callback prepares the
   required whitebox materials and project settings. The standalone batch-mode
   entry point is `Underpass.Editor.TrialBuild.Web`; the build service can use
   its regular WebGL build flow with the committed scene list.
5. Trigger one build, inspect the complete log, then download and serve the
   output with its accompanying files. This first build validates Unity source,
   shader inclusion, module requirements and project setup.
6. Enable **Auto-build** in the target's Scheduling settings and save. The
   GitHub connection also needs webhook permission: for a fine-grained token,
   **Contents: Read-only** and **Webhooks: Read and write** on this repository.
   A classic token needs `write:repo_hook` in addition to its source access.
   Keep the token in Unity's credential store. Manual builds succeeding does not
   prove automatic triggering works. Check GitHub repository Settings → Webhooks
   for the Unity hook and its latest push delivery; verify branch `main`.
   Review the service's current usage limits and plan. Automatic build triggering
   was verified by build 5: successful `fd065c5`, trigger `polling`.

## Then: native iOS

- Complete Apple Developer Program enrollment and create the app identifier.
- Configure signing credentials/provisioning in the build service's secret store.
- Create an iOS target on a supported macOS/Xcode builder using the same source.
- The proposed bundle ID is `com.lukechn99.underpass`; confirm it in the Apple
  account before making it permanent. Set unique build numbers for uploads.
- `Underpass.Editor.TrialBuild.IOS` exports the Xcode project. Cloud service/Xcode
  signing and a separate upload step deliver the app to App Store Connect.
- Add the user as an internal tester, distribute through internal TestFlight,
  then install/test on iPhone 15 Pro. Upload processing remains necessary.

Native build signing and TestFlight upload are **not yet configured**.
The user's Mac/Xcode remains an optional setup/debugging aid, not a required
always-running build host.

## Automated access from this workspace

The dashboard project URL is not API authorization. If build triggering/log
inspection should run from the assistant workspace, configure a Unity service
account with project-scoped Build Automation permissions. Place its key ID and
secret in the environment's secret settings (never chat or Git). Unity's current
v3 API uses service-account Basic authentication; the legacy build API key is not
supported there. No Unity credentials are currently configured here.

## References

- [Configure Build Automation](https://docs.unity.com/en-us/build-automation/basic-build-configuration/overview)
- [Connect Git source](https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system)
- [Build Automation API](https://docs.unity.com/en-us/oas-build-automation-client/3.0.0)
- [Sign an iOS app](https://docs.unity.com/en-us/build-automation/sign-build-artifacts/sign-an-ios-application)
- [Internal TestFlight testing](https://developer.apple.com/help/app-store-connect/test-a-beta-version/add-internal-testers/)
