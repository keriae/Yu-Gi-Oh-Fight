# Compile-only MDPro3 references

Build with the pinned .NET SDK, then pass publisher Managed, net48 reference directory, a separate output directory, and NETStandard 2.1 reference DLL.

The tool restores compiler metadata removed by Unity stripping. Its output stays in build/mdpro3-source/references and is never shipped as a runtime dependency. The source build requires every generated external type/member reference to resolve against untouched publisher Managed libraries.

Supplemented declarations and their sources are recorded in reference-prep.json. Original source notices and publisher libraries remain intact.
