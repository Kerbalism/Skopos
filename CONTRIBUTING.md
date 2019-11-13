# Contributing

All Contributions welcome. Just create a PR on GitHub.

# Building

I use VisualStudio, community edition. You can get that for free.

The project is set up to compile against KSP 1.8 and up, you might have to
some legwork if you need to build it for a previous version.

Near the top of src/KerbalismContracts/KerbalismContracts.csproj there are
two path definitions you need to adapt for your KSP installation directory
and the location of the KSP dll files within. The current presets at the time
of writing this work on MacOS, if you use a different platform you should be
able to build pretty easily. Just change KSPDevPath and KSPDevDllsRelativePath
to your needs.

# Dependencies

## Kerbalism

You need Kerbalism 1.3 or newer. Kerbalism ships a BootLoader.dll which then, at
runtime of KSP, loads the contents of one of the distributed kbin files. This
is how Kerbalism can support multiple KSP versions with just one zip distribution.

However, Visual Studio will look for a Kerbalism.dll file instead. To build
the KerbalismContracts project, rename one of the downloaded Kerbalism*.kbin
files (f.i. Kerbalism18.kbin) to Kerbalism.dll.

## Contract Configurator

You need to install ContractConfigurator to your GameData. If it is there,
the project should be able to find it.
