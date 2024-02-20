using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2LuaDoc.Core;

public static class Extension
{
    public static bool TryVisitSummary(this XmlDocument document, out string summary)
    {
        var ret = TryVisitNodeFind(document, "summary", out summary);
        if (ret)
        {
            summary = summary.Replace("\r\n", "\n");
            summary = summary.Trim();
        }
        return ret;
    }
    
    public static bool TryVisitParam(this XmlDocument document, string paramName, out string summary)
    {
        var ret = TryVisitNodeFind(document, "param", out summary, "name", paramName);
        if (ret)
        {
            summary = summary.Replace("\r\n", "\n");
            summary = summary.Trim();
        }
        return ret;
    }
    
    public static bool TryVisitReturns(this XmlDocument document, out string summary)
    {
        var ret = TryVisitNodeFind(document, "returns", out summary);
        if (ret)
        {
            summary = summary.Replace("\r\n", "\n");
            summary = summary.Trim();
        }
        return ret;
    }

    public static string TransToSingleLine(this string str, bool transNewLineToBr = true)
    {
        str = str.Trim().Replace("\r", string.Empty);
        if (transNewLineToBr)
        {
            str = str.Replace("\n", "<br>");
        }
        else
        {
            str = str.Replace("\n", string.Empty);
        }
        
        return str;
    }
    
    public static StringBuilder AppendIndent(this StringBuilder builder, int indent)
    {
        for (var i = 0; i < indent; i++)
        {
            builder.Append("    ");
        }
        return builder;
    }
    
    public static StringBuilder AppendIndentLine(this StringBuilder builder, int indent, string str)
    {
        builder.AppendIndent(indent);
        builder.AppendLine(str);
        return builder;
    }

    private static bool TryVisitNodeFind(XmlNode? node, string nodeName, out string summary)
    {
        if (node == null)
        {
            summary = string.Empty;
            return false;
        }
        
        summary = string.Empty;
        if (node is XmlElement element)
        {
            if (element.Name == nodeName)
            {
                summary = element.InnerText;
                return true;
            }
        }

        if (node.HasChildNodes)
        {
            foreach (var childNode in node.ChildNodes)
            {
                if (TryVisitNodeFind(childNode as XmlNode, nodeName, out summary))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryVisitNodeFind(XmlNode? node, string nodeName, out string summary, string attrName, string paramName)
    {
        if (node == null)
        {
            summary = string.Empty;
            return false;
        }
        
        summary = string.Empty;
        if (node is XmlElement element)
        {
            if (element.Name == nodeName)
            {
                if (element.HasAttribute(attrName))
                {
                    if (element.GetAttribute(attrName) == paramName)
                    {
                        summary = element.InnerText;
                        return true;
                    }
                }
            }
        }

        if (node.HasChildNodes)
        {
            foreach (var childNode in node.ChildNodes)
            {
                if (TryVisitNodeFind(childNode as XmlNode, nodeName, out summary, attrName, paramName))
                {
                    return true;
                }
            }
        }

        return false;
    }
}