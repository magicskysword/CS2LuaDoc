using Cysharp.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace CS2LuaDoc.Core;

public class CSMetaDataLoader
{
    public CSMetaDataLoader(SolutionLoader solutionLoader)
    {
        SolutionLoader = solutionLoader;
    }
    
    public SolutionLoader SolutionLoader { get; }
    
    public async UniTask<ProjectMetaData> LoadAsync()
    {
        var projectMetaData = new ProjectMetaData();
        if (!SolutionLoader.IsLoaded)
        {
            await SolutionLoader.LoadAsync();
        }

        if (!SolutionLoader.IsLoadSuccess)
        {
            throw new Exception("工程加载失败");
        }

        foreach (var compilation in SolutionLoader.CachedCompilations)
        {
            var visitor = new ClassDeclarationVisitor();
            visitor.Visit(compilation.GlobalNamespace);
            
            foreach (var classSymbol in visitor.classDeclarations)
            {
                if(classSymbol.IsImplicitClass)
                    continue;
                
                if(classSymbol.IsAnonymousType)
                    continue;
                
                if(classSymbol.Name.StartsWith("<"))
                    continue;
                
                if(classSymbol.Name.Contains("="))
                    continue;
                
                var classMetaData = new ClassMetaData();
                RejectClassMetaData(classMetaData, classSymbol);
                projectMetaData.ClassMetaDataList.Add(classMetaData);
            }
        }

        return projectMetaData;
    }

    private string? GetFullNamespace(INamedTypeSymbol classSymbol)
    {
        var namespaceSymbol = classSymbol.ContainingNamespace;
        if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
        {
            return null;
        }
        
        var namespaceName = namespaceSymbol.Name;
        var parentNamespace = namespaceSymbol.ContainingNamespace;
        while (parentNamespace != null && !parentNamespace.IsGlobalNamespace)
        {
            namespaceName = $"{parentNamespace.Name}.{namespaceName}";
            parentNamespace = parentNamespace.ContainingNamespace;
        }
        
        return namespaceName;
    }

    private void RejectBaseMetaData(BaseMetaData metaData, ISymbol symbol)
    {
        metaData.Name = symbol.Name;
        var rawRemark = symbol.GetDocumentationCommentXml();
        metaData.RawRemark = rawRemark ?? string.Empty;
    }
    
    private void RejectClassMetaData(ClassMetaData classMetaData, INamedTypeSymbol classSymbol)
    {
        RejectBaseMetaData(classMetaData, classSymbol);
        classMetaData.Namespace = GetFullNamespace(classSymbol);
        classMetaData.IsPublic = classSymbol.DeclaredAccessibility == Accessibility.Public;
        // 构造函数
        classMetaData.ConstructorMetaDataList.AddRange(classSymbol.Constructors.Select(constructorSymbol =>
        {
            var constructorMetaData = new MethodMetaData();
            RejectMethodMetaData(constructorMetaData, constructorSymbol);
            return constructorMetaData;
        }));
        
        var members = classSymbol.GetMembers();
        foreach (var member in members)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                {
                    var fieldSymbol = (IFieldSymbol) member;
                    var fieldMetaData = new FieldMetaData();
                    RejectFieldMetaData(fieldMetaData, fieldSymbol);
                    classMetaData.FieldMetaDataList.Add(fieldMetaData);
                    break;
                }
                case SymbolKind.Event:
                {
                    var eventSymbol = (IEventSymbol) member;
                    var eventMetaData = new EventMetaData();
                    RejectEventMetaData(eventMetaData, eventSymbol);
                    classMetaData.EventMetaDataList.Add(eventMetaData);
                    break;
                }
                case SymbolKind.Property:
                {
                    var propertySymbol = (IPropertySymbol) member;
                    var propertyMetaData = new PropertyMetaData();
                    RejectPropertyMetaData(propertyMetaData, propertySymbol);
                    classMetaData.PropertyMetaDataList.Add(propertyMetaData);
                    break;
                }
                case SymbolKind.Method:
                {
                    var methodSymbol = (IMethodSymbol) member;
                    if(methodSymbol.Name.StartsWith("get_") || methodSymbol.Name.StartsWith("set_"))
                        continue;
                    
                    if(methodSymbol.Name == ".ctor")
                        continue;
                    
                    var methodMetaData = new MethodMetaData();
                    RejectMethodMetaData(methodMetaData, methodSymbol);
                    classMetaData.MethodMetaDataList.Add(methodMetaData);
                    break;
                }
            }
        }
    }

    private void RejectEventMetaData(EventMetaData eventMetaData, IEventSymbol eventSymbol)
    {
        RejectBaseMetaData(eventMetaData, eventSymbol);
        eventMetaData.Type = eventSymbol.Type;
        eventMetaData.IsPublic = eventSymbol.DeclaredAccessibility == Accessibility.Public;
    }

    private void RejectFieldMetaData(FieldMetaData fieldMetaData, IFieldSymbol fieldSymbol)
    {
        RejectBaseMetaData(fieldMetaData, fieldSymbol);
        fieldMetaData.Type = fieldSymbol.Type;
        fieldMetaData.IsPublic = fieldSymbol.DeclaredAccessibility == Accessibility.Public;
    }
    
    private void RejectPropertyMetaData(PropertyMetaData propertyMetaData, IPropertySymbol propertySymbol)
    {
        RejectBaseMetaData(propertyMetaData, propertySymbol);
        propertyMetaData.Type = propertySymbol.Type;
        propertyMetaData.IsPublic = propertySymbol.DeclaredAccessibility == Accessibility.Public;
    }
    
    private void RejectMethodMetaData(MethodMetaData methodMetaData, IMethodSymbol methodSymbol)
    {
        RejectBaseMetaData(methodMetaData, methodSymbol);
        methodMetaData.ReturnType = methodSymbol.ReturnType;
        methodMetaData.IsPublic = methodSymbol.DeclaredAccessibility == Accessibility.Public;
        if (methodSymbol.IsExtensionMethod)
        {
            // 扩展方法在xlua里调用时为实例方法
            methodMetaData.IsStatic = false;
            // 扩展方法的第一个参数是扩展的类型
            foreach (var parameterSymbol in methodSymbol.Parameters.Skip(1))
            {
                var parameterMetaData = new MethodParameterMetaData();
                RejectMethodParameterMetaData(parameterMetaData, parameterSymbol);
                methodMetaData.Parameters.Add(parameterMetaData);
            }
        }
        else
        {
            methodMetaData.IsStatic = methodSymbol.IsStatic;
            foreach (var parameterSymbol in methodSymbol.Parameters)
            {
                var parameterMetaData = new MethodParameterMetaData();
                RejectMethodParameterMetaData(parameterMetaData, parameterSymbol);
                methodMetaData.Parameters.Add(parameterMetaData);
            }
        }
        
        if (methodSymbol.IsGenericMethod)
        {
            methodMetaData.IsGenericMethod = true;
            foreach (var typeParameterSymbol in methodSymbol.TypeParameters)
            {
                var typeParameterMetaData = new MethodTypeParameterMetaData();
                RejectMethodTypeParameterMetaData(typeParameterMetaData, typeParameterSymbol);
                methodMetaData.TypeParameters.Add(typeParameterMetaData);
            }
        }
    }

    private void RejectMethodTypeParameterMetaData(MethodTypeParameterMetaData typeParameterMetaData, ITypeParameterSymbol typeParameterSymbol)
    {
        RejectBaseMetaData(typeParameterMetaData, typeParameterSymbol);
        typeParameterMetaData.ConstraintTypes.AddRange(typeParameterSymbol.ConstraintTypes);
    }

    private void RejectMethodParameterMetaData(MethodParameterMetaData parameterMetaData, IParameterSymbol parameterSymbol)
    {
        RejectBaseMetaData(parameterMetaData, parameterSymbol);
        parameterMetaData.Type = parameterSymbol.Type;
        parameterMetaData.IsRef = parameterSymbol.RefKind == RefKind.Ref;
        parameterMetaData.IsOut = parameterSymbol.RefKind == RefKind.Out;
        parameterMetaData.IsParams = parameterSymbol.IsParams;
    }
}