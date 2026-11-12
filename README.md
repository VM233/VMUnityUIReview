# VM Unity UI Review

`com.vm233.unity-ui-review` owns reusable Unity UI Toolkit authoring, native
button feedback, runtime experience, UI Builder and transition reviews. It
extends the existing Automation catalog through the official Unity CLI and
Pipeline; Automation owns input, capture, request binding and durable jobs.

Install both this package and `com.vm233.unity-automation` using full remote
Git commit SHAs in the project's `Packages/manifest.json`. Automation's required
version is declared in [package.json](package.json).

```json
{
  "dependencies": {
    "com.vm233.unity-automation": "https://github.com/VM233/VMUnityAutomation.git#<full-automation-commit-sha>",
    "com.vm233.unity-ui-review": "https://github.com/VM233/VMUnityUIReview.git#<full-ui-review-commit-sha>"
  }
}
```

The declared minimum is Unity 2021.3.18f1. Native capability and qualification
boundaries are documented with each review. `Runtime` is Player compatible;
`Editor` owns authoring and CLI integration, and `Tests` contains native controls.

Discover the current review contracts through the project's official CLI catalog.
Review exact edited paths with complete template, theme and consumer roots.
Read [the review documentation](Documentation~/index.md),
[migration guide](Documentation~/migration.md), and [release history](CHANGELOG.md).
Passing a measured check does not certify visual quality or an untested device.

Made with AnkleBreaker MCP. Distributed under the
[AnkleBreaker Open License v1.0](LICENSE).

![AnkleBreaker MCP](https://raw.githubusercontent.com/AnkleBreaker-Studio/unity-mcp-plugin/main/icon.png)
