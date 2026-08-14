# Alanta.Aspire.Hosting.EasyAuthProxy: packaging design

`AddEasyAuthProxy()` runs the proxy as a plain .NET process, not a container. This document
explains how that's wired up and the failure modes hit (and fixed) while building it, so the next
person touching this doesn't have to rediscover them by hitting the same walls.

## Why not just reference a container image?

`AddEasyAuthProxyContainer()` still exists and works (pulls `ghcr.io/alanta/easyauthdevproxy`),
but it's no longer the default. EasyAuthDevProxy is a plain, portable ASP.NET Core/YARP app with
no OS-specific dependencies — pulling a container image for it is real overhead (image pull,
Docker/Podman as a hard requirement, the `host.docker.internal`/`host.containers.internal` podman
patch-up, the OTLP-cert-trust workaround) for something that can just run as a subprocess.

An earlier attempt at an executable-based path (`AddEasyAuthProxyExecutable`, removed in commit
`ec09cdb`) got this partially right but was fragile: it hardcoded the NuGet global-packages path
(`~/.nuget/packages/alanta.easyauthdevproxy/1.0.0/tools`) and shipped self-contained, per-RID
builds (`linux-x64;win-x64;osx-x64`) bundled together into one ~49MB NuGet package. The current
design fixes both problems.

## How the proxy binaries get to the consumer

1. `EasyAuthDevProxy.csproj` is a normal framework-dependent build — no
   `RuntimeIdentifiers`/self-contained publish for the tool path (RID-specific settings are only
   used for the separate container-image publish, via `ContainerRuntimeIdentifier`).
2. `Alanta.Aspire.Hosting.EasyAuthProxy.csproj` publishes `EasyAuthDevProxy` (framework-dependent,
   portable) into `obj/proxy-publish/` and bundles that output **inside its own package** at the
   package root under `proxy/` — not as a second NuGet package with its own version to keep in
   sync.
3. An auto-imported `build/Alanta.Aspire.Hosting.EasyAuthProxy.targets` file (NuGet's
   `build/<PackageId>.targets` convention — auto-imported into any project that
   `PackageReference`s this package) copies `proxy/` into the *consuming* project's own output
   directory on every build. This is what makes `dotnet add package Alanta.Aspire.Hosting.EasyAuthProxy`
   "just work" with no wiring on the consumer's end. **The file name must track `PackageId`, not the
   project name** — this file was renamed when the PackageId moved off the `Aspire.*` prefix
   (which nuget.org reserves for Microsoft) to `Alanta.Aspire.Hosting.EasyAuthProxy`; if `PackageId`
   ever changes again, this file has to be renamed to match or the auto-import silently stops
   working.
4. `EasyAuthProxyResourceBuilderExtensions.ResolveProxyDllPath()` finds the bundled DLL at run
   time by looking next to its own assembly (`Assembly.Location`), checking two candidate
   locations: a `proxy/` sibling folder (works for both a real PackageReference consumer, via step
   3, and this project's own build output) and `../../proxy/` (the packed NuGet layout, in case the
   assembly is somehow loaded from inside the nupkg's `lib/<tfm>/` directly). No hardcoded
   NuGet-cache paths, no version-string assumptions.
5. **In-repo `ProjectReference` consumers** (i.e. `AspireDemo.AppHost` in this repo) don't get step
   3 for free — NuGet's `build/*.targets` auto-import only fires for restored packages, not plain
   `ProjectReference`s. `AspireDemo.AppHost.csproj` has its own small `AfterTargets="Build"` copy
   target mirroring the same logic for exactly this reason. If you add another in-repo AppHost
   that references `Alanta.Aspire.Hosting.EasyAuthProxy` via `ProjectReference`, copy that target too.

## Known rough edges

- **Packed nupkg has a duplicate `proxy/build/*` tree.** NuGet's pack task appears to
  auto-mirror non-`lib` content under `build/` once *any* item with a `build\` `PackagePath` is
  present in the package (legacy MSBuild-props-package compat behavior, not something we asked
  for). This roughly doubles the proxy's footprint inside the `.nupkg` file. It's harmless — the
  runtime resolver only ever reads the top-level `proxy/` copy — but worth fixing if anyone wants
  to dig into exactly which NuGet SDK behavior triggers it.
- **Third-party dependencies are bundled, not just referenced.** Because we publish and embed
  EasyAuthDevProxy's full managed dependency closure (currently Yarp, Azure.Core,
  Microsoft.ApplicationInsights, OpenTelemetry.*, Microsoft.Identity.Client, etc. — check
  `EasyAuthDevProxy.csproj`'s `PackageReference`s for the current list), we are the redistributor
  of those binaries, not NuGet resolving them from the original publisher. All current
  dependencies are MIT or Apache-2.0, which permit this, but it's why
  `Alanta.Aspire.Hosting.EasyAuthProxy/THIRD-PARTY-NOTICES.txt` exists and is packed alongside the
  README. **If you add a new `PackageReference` to `EasyAuthDevProxy.csproj`, add it to that
  notices file too** (component name + copyright holder + license family; no version numbers
  needed — see below).
- **No automated check ties the notices file to the actual dependency list.** It's manually
  maintained. A CI step that diffs `dotnet list package` output for `EasyAuthDevProxy` against
  `THIRD-PARTY-NOTICES.txt` would close this gap but doesn't exist yet.
- **No runtime clash risk despite the bundling.** The proxy runs as a separate OS process
  (`dotnet proxy/EasyAuthDevProxy.dll`) with its own `.deps.json`/`.runtimeconfig.json` — it never
  shares an AppDomain/AssemblyLoadContext with the AppHost process, so whatever
  `Microsoft.Extensions.*`/`OpenTelemetry.*` versions Aspire.Hosting itself uses in the AppHost
  can't collide with the proxy's own versions.
- **Notices file intentionally omits version numbers.** Neither MIT nor Apache-2.0 requires them
  (only copyright notice + license text, and for Apache-2.0, NOTICE-file reproduction — none of
  these upstream packages ship a NOTICE file). Versions would just go stale every time a
  dependency gets bumped in `EasyAuthDevProxy.csproj`, with no compliance upside.

## Aspire executable-resource gotchas hit while building this

These aren't specific to this repo, but they cost real time to diagnose and aren't obviously
documented anywhere obvious, so noting them here:

- `WithHttpEndpoint(name:, env:)`'s `env:` shortcut only injects the **bare port number** into
  that env var for `ExecutableResource` — project resources get a fully-formed URL for free,
  executables don't. Kestrel will reject a bare port number as an invalid `ASPNETCORE_URLS` value.
  Build the URL explicitly instead: `context.EnvironmentVariables["ASPNETCORE_URLS"] =
  $"http://+:{endpoint.TargetPort}"` inside a `WithEnvironment(context => ...)` callback.
- `WithHttpEndpoint` needs an explicit `targetPort:` for executable resources. Without it, DCP
  fails to create the endpoint's service object ("information about the port to expose the service
  is missing; service-producer annotation is invalid") and the resource never leaves `Finished`
  state. Containers had this working already (`targetPort: 8080`); executables need the same.
- Don't invoke `<MSBuild Targets="Publish">` (the MSBuild task, not the CLI) against a project
  that's *also* reached via a `ProjectReference` in the same build graph — it deadlocks the
  MSBuild engine (observed: over an hour with zero output, no error). Use `<Exec Command="dotnet
  publish ...">` (an out-of-process invocation) instead, as `PublishEasyAuthDevProxy` does in
  `Alanta.Aspire.Hosting.EasyAuthProxy.csproj`.
- Dynamically adding `None` items with `CopyToOutputDirectory` metadata inside a custom
  `BeforeTargets="AssignTargetPaths"` target is fragile — `AssignTargetPaths` can recompute/collide
  target paths in ways that silently drop most of the files. Prefer an explicit `<Copy
  SourceFiles="@(...)" DestinationFolder="..."/>` in a plain `AfterTargets="Build"` target instead;
  it's more verbose but predictable.
- For getting a package's bundled content copied into a *consumer's* output directory, NuGet's
  `contentFiles` convention (`PackagePath="contentFiles\any\<tfm>\..."` + `BuildAction`/
  `CopyToOutput` item metadata) looked like the intended mechanism but its `copyToOutput` flag
  wasn't actually being honored by the SDK version used here (10.0.302) — the generated nuspec
  never got a `copyToOutput="true"` attribute no matter how the metadata was set. The
  `build/<PackageId>.targets` auto-import convention (see above) is the mechanism actually used,
  and it's more battle-tested generally.

## Debugging tools that mattered

- `aspire run` / `aspire describe` / `aspire logs <resource>` / `aspire stop` give resource-level
  state and per-resource logs that a plain `dotnet run` on the AppHost doesn't surface as cleanly
  — `aspire describe` shows each resource's `State`/`Health` at a glance (e.g. `Running/Healthy`
  vs `Finished`), and `aspire logs <name>` gets you that resource's stdout/stderr including the
  actual unhandled-exception stack trace when a child process crashes on startup.
- If `dotnet restore`/`build`/`pack` hangs indefinitely at "Determining projects to restore..."
  with zero further output, check for a stuck NuGet HTTP-cache lock before assuming anything about
  your own MSBuild changes: `strace -f -e trace=file <the hung command>` and look for a `mkdir
  .../NuGetScratch<user>/lock` looping with `EEXIST`. This happens when a process holding that
  lock got killed (e.g. `kill -9`) or got stuck/orphaned without releasing it. Fix: find and kill
  the stuck holder, then `rm -rf /tmp/NuGetScratch<user>` and retry. This repo's dev environment
  has been observed accumulating dozens of orphaned, stopped `aspire-managed nuget search`
  processes (spawned by Aspire CLI / editor tooling version checks) that hold exactly this lock
  indefinitely — `ps aux | grep aspire-managed` is worth checking first if a build hangs for no
  obvious reason.
