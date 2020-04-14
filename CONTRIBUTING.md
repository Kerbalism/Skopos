# Contributing

All Contributions welcome. Just create a PR on GitHub.

# Building the Plugin

I use VisualStudio, community edition. You can get that for free.

Near the top of src/KerbalismContracts/KerbalismContracts.csproj there are
two path definitions you need to adapt for your KSP installation directory
and the location of the KSP dll files within. The current presets at the time
of writing this work on MacOS, if you use a different platform you should be
able to build pretty easily. Just change KSPDevPath and KSPDevDllsRelativePath
to your needs.

## Dependencies

* Kerbalism (read below!)
* Contract Configurator

## Kerbalism

You need Kerbalism 4.0 or newer. Kerbalism ships a BootLoader.dll which then, at
runtime of KSP, loads the contents of one of the distributed kbin files. This
is how Kerbalism can support multiple KSP versions with just one zip distribution.

However, Visual Studio will look for a Kerbalism.dll file instead. To build
the KerbalismContracts project, rename one of the downloaded Kerbalism*.kbin
files (f.i. Kerbalism18.kbin) to Kerbalism.dll.

## Contract Configurator

You need to install ContractConfigurator to your GameData. If it is there,
the project should be able to find it.


# Contracts

Some random notes:

* Due to the way ContractConfigurator works, all contract parameters that are
  supposed to work with unloaded vessels must be in a VesselParameterGroup.
