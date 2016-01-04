//Modify to change to a different scriptcs version
#r ".\.svm\versions\0.15.0\ScriptCs.Hosting.dll"
#r ".\.svm\versions\0.15.0\ScriptCs.Engine.Roslyn.dll"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Hosting;

public class InteractiveHostAssemblyResolver : IAssemblyResolver {
	private IAssemblyResolver _resolver;

	public InteractiveHostAssemblyResolver(
            IFileSystem fileSystem,
            IPackageAssemblyResolver packageAssemblyResolver,
            IAssemblyUtility assemblyUtility,
            ILogProvider logProvider)
	{
		_resolver = new AssemblyResolver(fileSystem, packageAssemblyResolver,assemblyUtility,logProvider);
	}

    public IEnumerable<string> GetAssemblyPaths(string path, bool binariesOnly = false)
    {
        return _resolver.GetAssemblyPaths(path, binariesOnly).Where(p=>!p.Contains("ScriptCs.Contracts"));
    }
}

private class ScriptCsHost
{
    private IScriptExecutor _executor;
    private ScriptServices _services;
    private IDictionary<string, IScriptPackContext> _contexts;
    private IConsole _console;
    private IFileSystem _fs;
    private IEnumerable<IScriptPack> _scriptPacks;

    public ScriptCsHost()
    {
        var console = (IConsole)new ScriptConsole();
        _console = console;
        var logProvider = new ColoredConsoleLogProvider(LogLevel.Info, console);
        
        var overrides = new Dictionary<Type,Object>();
        overrides[typeof(IAssemblyResolver)] = typeof(InteractiveHostAssemblyResolver);
        var initializationServices = new InitializationServices(logProvider, overrides);
        var builder = new ScriptServicesBuilder(console, logProvider, initializationServices:initializationServices);
        builder.ScriptEngine<RoslynScriptEngine>();
        builder.Repl(false);
        builder.LoadScriptPacks();
        var services = builder.Build();
        
        _services = services;
        _fs = services.FileSystem;
        _executor = services.Executor;  

        var resolver = services.AssemblyResolver;
        var workingDirectory = _fs.CurrentDirectory;
        services.ScriptLibraryComposer.Compose(workingDirectory);
        var assemblies = resolver.GetAssemblyPaths(workingDirectory);

        var scriptPacks = _services.ScriptPackResolver.GetPacks().ToArray();
        GetScriptPackContexts(scriptPacks);
        _executor.Initialize(assemblies, scriptPacks);

    }

    public object Execute(string script)
    {
        var result = _executor.ExecuteScript(script);
        if (result.CompileExceptionInfo != null)
        {
            result.CompileExceptionInfo.Throw();
        }

        if (result.ExecuteExceptionInfo != null)
        {
            result.ExecuteExceptionInfo.Throw();
        }
        return result.ReturnValue;
    }

    private void GetScriptPackContexts(IEnumerable<IScriptPack> scriptPacks)
    {
        _contexts = new Dictionary<string, IScriptPackContext>();
        foreach (var pack in scriptPacks)
        {
            var context = pack.GetContext();
            _contexts[context.GetType().Name] = context;
        }
    }

    public T Require<T>()
    {
        return (T) _contexts[typeof(T).Name];
    }

    public dynamic Require(string scriptPack) {
    	return _contexts[scriptPack];
    }
    
    private IEnumerable<string> PreparePackages(IPackageAssemblyResolver packageAssemblyResolver, IPackageInstaller packageInstaller, IEnumerable<IPackageReference> additionalNuGetReferences, IEnumerable<string> localDependencies, ILog logger)
    {
        var workingDirectory = _fs.CurrentDirectory;

        var packages = packageAssemblyResolver.GetPackages(workingDirectory);
        packages = packages.Concat(additionalNuGetReferences);

        try
        {
            packageInstaller.InstallPackages(
                packages, true);
        }
        catch (Exception e)
        {
            logger.ErrorFormat("Installation failed: {0}.", e.Message);
        }
        var assemblyNames = packageAssemblyResolver.GetAssemblyNames(workingDirectory);
		assemblyNames = assemblyNames.Concat(localDependencies);
        return assemblyNames;
    }

}

var scriptcs = new ScriptCsHost();
