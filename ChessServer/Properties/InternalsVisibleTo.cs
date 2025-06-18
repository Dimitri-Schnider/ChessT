using System.Runtime.CompilerServices;

// Damit Moq (DynamicProxyGenAssembly2) internale Member mocken kann:
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// Damit die Tests internale Member sehen können:
[assembly: InternalsVisibleTo("ChessServer.Tests")]