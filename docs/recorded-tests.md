# Recorded Testing in `microsoft/mcp`

## Context

This repository ships CLI tools. Specifically, multiple combinations of `tools` assembled into `mcp servers` that are effectively standalone CLI tools themselves.

This complicates the record/playback story somewhat, as the process code itself is running _separately_ during our livetesting. If your tool's livetests are based upon `CommandTestsBase` from `Azure.Mcp.Tests`, then you'll be familiar with this process:

On each live test:
 - New up test class, instantiate `Settings` from a locally found `.livetestsettings.json` (created by `./eng/scripts/Deploy-TestResources.ps1`).
 - Set appropriate `environment` variable settings to match needs of livetest, ensuring that environment is primed to use the logged in powershell credential automatically.
 - Call an `mcp server` as a CLI command, passing it specific instructions formatted within the test.
 - `mcp server` uses prepared local env for auth, completes the task, and returns output to the test
 - Test asserts on output of tool

Notably, in the above process, the call of the tool is calling into a prebuilt EXE, there is no opportunity for httpclient injection or the like that would allow recordings to be made from tool calls. This was solved for this repo by the following.

- During **only** `debug` builds `HttpClientService` has been enhanced to support `redirect` automagically based upon the presence of `TEST_PROXY_URL` variable.

## Converting a `LiveTest` to a `Recorded` test


## The sanitization/playback loop

- If a `.livesettings` file exists at root of your `tool`, continue.
- Update the `TestMode` setting within the `.livesettings.json` alongside each livetests csproj to `Record`.
- Invoke the livetests.
- Use `.proxy/Azure.Sdk.Tools.TestProxy(.exe) config locate -a path/to/assets.json` to locate the recordings directory
  - Review recordings for secrets.
- Temporarily rename the `livesettings.json` to a different name to force `playback` mode. I use `DISABLED.livesettings.json`.
- Invoke the livetests again. If they all pass, and secret review was clean, push.
    - `.proxy/Azure.Sdk.Tools.TestPRoxy(.exe) push -a path/to/assets.json`
    - If they _do not_ pass, examine each individual test error. There should be some sort of mismatch or the like in the test-proxy error.
- Commit the `assets.json` updated with the new tag.

