# AGENTS.md

Guidance for agents working in this repo. Project overview and usage docs live in
[README.md](README.md) — this file is about *working in* the repo, not using the proxy.

## Repo layout

| Path | What it is |
|---|---|
| `EasyAuthDevProxy/` | The proxy itself: ASP.NET Core + YARP reverse proxy, simulates Azure Container Apps EasyAuth for local dev. |
| `EasyAuthDevProxy.Tests/` | xUnit tests for the proxy (`Microsoft.AspNetCore.Mvc.Testing`, FluentAssertions). |
| `Alanta.Aspire.Hosting.EasyAuthProxy.Tests/` | xUnit tests for the hosting integration. Application-model only — they build the model in memory and assert on annotations, no DCP, containers or dashboard. |
| `Alanta.Aspire.Hosting.EasyAuthProxy/` | The Aspire hosting integration (`AddEasyAuthProxy()` / `AddEasyAuthProxyContainer()`). Published as the `Alanta.Aspire.Hosting.EasyAuthProxy` NuGet package — the project/folder/assembly all share this name; only the C# namespace stays `Aspire.Hosting` (via an explicit `RootNamespace`), so the extension methods are discoverable without an extra `using`. See [docs/aspire-hosting-packaging.md](docs/aspire-hosting-packaging.md) for how it bundles and ships the proxy. |
| `AspireDemo/` | A working Aspire sample (`AppHost` + `App` + `ServiceDefaults`) exercising the hosting integration end-to-end. Use this to manually verify changes to `Alanta.Aspire.Hosting.EasyAuthProxy` — see below. |
| `DemoApp/` | An older, minimal demo project. Not part of the Aspire sample; check before assuming it's exercised by anything. |
| `docs/` | Deeper design-decision write-ups that don't belong in a README. |
| `packages/`, `dist/` | Local NuGet feeds used for manual testing (gitignored, not part of the build). |

Solution file is `EasyAuthDevProxy.slnx` (the newer XML-based `.slnx` format, not `.sln`).

## Building and testing

```
dotnet build EasyAuthDevProxy.slnx -c Debug
dotnet test EasyAuthDevProxy.slnx -c Release
```

Requires the .NET 10 SDK. `Alanta.Aspire.Hosting.EasyAuthProxy.csproj` and `AspireDemo.AppHost.csproj`
both trigger a `dotnet publish` of `EasyAuthDevProxy` as part of their own build (see the packaging
doc) — expect a nested restore/build line for it in the output; that's expected, not a duplicate
build bug.

**If a `dotnet restore`/`build`/`pack` hangs indefinitely at "Determining projects to restore..."**
with no further output, don't assume it's your change — check for a stuck NuGet HTTP-cache lock
first. Full diagnosis and fix in [docs/aspire-hosting-packaging.md](docs/aspire-hosting-packaging.md#debugging-tools-that-mattered).

## Manually verifying the Aspire integration

`dotnet build`/`dotnet test` passing does **not** prove the AppHost actually runs — the hosting
integration involves MSBuild packaging tricks and Aspire/DCP process orchestration that only show
up at run time. After touching anything in `Alanta.Aspire.Hosting.EasyAuthProxy/`, verify it for real:

```
cd AspireDemo/AspireDemo.AppHost
aspire run
```

Then, from another shell:

```
aspire describe          # per-resource State/Health at a glance
aspire logs easyauth     # the proxy's own stdout/stderr, including startup exceptions
aspire stop
```

Plain `dotnet run` also starts the AppHost but doesn't register it with the `aspire` CLI, so
`aspire describe`/`logs`/`stop` won't find it afterwards — prefer `aspire run` when you'll need
those.

## CI

`.github/workflows/build.yaml`: on every push/PR, builds + tests the solution and packs
`Alanta.Aspire.Hosting.EasyAuthProxy` (version from GitVersion, Mainline mode) as a build artifact —
this also serves as the installable preview package for PRs. Publishing only happens when a
`vX.Y.Z` tag is pushed: that triggers the `release` job, which pushes the `.nupkg` to nuget.org,
creates a GitHub Release, and — only if `EasyAuthDevProxy/` actually changed since the previous
release tag — rebuilds and pushes the container image to `ghcr.io/alanta/easyauthdevproxy`. The
`EasyAuthDevProxy` project itself is not packed/published as its own NuGet package by CI — its only
external distribution channels right now are the container image and being bundled inside
`Alanta.Aspire.Hosting.EasyAuthProxy` (see the packaging doc). To cut a release: `git tag vX.Y.Z && git
push --tags` on `main`.

## Conventions / things not to relearn the hard way

- **`AddEasyAuthProxy()` defaults to running the proxy as a plain .NET process, not a container.**
  `AddEasyAuthProxyContainer()` is the explicit opt-in for container-based execution. Don't assume
  older docs/commits describing "the container resource" as the default are still accurate — check
  [docs/aspire-hosting-packaging.md](docs/aspire-hosting-packaging.md) for why and how this works.
- **Never pin `targetPort` on the executable proxy resource's endpoint.** For a plain process the
  target port *is* the OS port Kestrel binds, so a hardcoded value collides with whatever else on
  the machine happens to hold it (issue #11), and `WithHostPort` only changes the externally
  advertised port. Aspire has to allocate it, and DCP needs an `env:` binding
  (`ASPNETCORE_HTTP_PORTS`) to pass the allocated value in - computing
  `ASPNETCORE_URLS` in an environment callback does *not* work, because the target port is still
  null when those callbacks run and DCP then refuses to create the endpoint at all. The container
  resource is the opposite case: its target ports are container-internal and safe to pin.
- **Resolve environment variables in tests with `DistributedApplicationOperation.Publish`, never
  `Run`.** In run mode the value providers wait for endpoints DCP would have allocated, which
  never happens in a unit test, so the call blocks forever rather than failing — a whole test run
  hangs with no output. `ResourceEnvironment.ResolveEnvironmentAsync` in the hosting tests wraps
  the publish-mode call; endpoint-derived values come back as placeholders like
  `{easyauth.bindings.http.targetPort}`, which is exactly what those tests assert on.
- `EasyAuthDevProxy.csproj` is deliberately **framework-dependent, no `RuntimeIdentifiers`** for
  normal builds — RID-specific settings (`ContainerRuntimeIdentifier`) only apply to the separate
  container-image publish target. Don't reintroduce a `RuntimeIdentifiers` list "to be safe"; it
  was previously the source of a ~49MB self-contained multi-RID NuGet package that got removed for
  exactly that reason.
- If you add a `PackageReference` to `EasyAuthDevProxy.csproj`, also update
  `Alanta.Aspire.Hosting.EasyAuthProxy/THIRD-PARTY-NOTICES.txt` — its dependency closure gets bundled and
  redistributed inside the `Alanta.Aspire.Hosting.EasyAuthProxy` NuGet package, so new dependencies need
  their license/copyright listed there. Check the license before assuming it's fine to bundle;
  MIT and Apache-2.0 are known-good, anything else needs a fresh look.
