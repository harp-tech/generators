using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace Harp.Generators;

internal abstract class TemplateBase
{
    private StringBuilder builder;   
    private CompilerErrorCollection errors;
    private string currentIndent = string.Empty;
    private Stack<int> indents;
    
    public virtual IDictionary<string, object> Session { get; set; }

    public string FileName { get; set; } = string.Empty;
    
    public StringBuilder GenerationEnvironment
    {
        get => builder ??= new StringBuilder();
        set => builder = value;
    }
    
    public CompilerErrorCollection Errors
    {
        get => errors ??= [];
        set => errors = value;
    }
    
    public string CurrentIndent => currentIndent;
    
    private Stack<int> Indents => indents ??= [];
    
    public ToStringInstanceHelper ToStringHelper { get; } = new();

    public abstract string TransformText();

    public virtual void Initialize() { }

    public void Initialize(string fileName, CompilerErrorCollection errors, Dictionary<string, object> session)
    {
        FileName = fileName;
        Errors = errors;
        Session = session;
        Initialize();
    }
    
    public void Error(string message)
    {
        Errors.Add(new CompilerError(FileName, -1, -1, null, message));
    }
    
    public void Warning(string message)
    {
        Errors.Add(new CompilerError(FileName, -1, -1, null, message)
        {
            IsWarning = true
        });
    }
    
    public string PopIndent()
    {
        if (Indents.Count == 0)
            return string.Empty;
        
        int lastPos = currentIndent.Length - Indents.Pop();
        string last = currentIndent.Substring(lastPos);
        currentIndent = currentIndent.Substring(0, lastPos);
        return last;
    }
    
    public void PushIndent(string indent)
    {
        Indents.Push(indent.Length);
        currentIndent += indent;
    }
    
    public void ClearIndent()
    {
        currentIndent = string.Empty;
        Indents.Clear();
    }
    
    public void Write(string textToAppend)
    {
        GenerationEnvironment.Append(textToAppend);
    }
    
    public void Write(string format, params object[] args)
    {
        GenerationEnvironment.AppendFormat(format, args);
    }
    
    public void WriteLine(string textToAppend)
    {
        GenerationEnvironment.Append(currentIndent);
        GenerationEnvironment.AppendLine(textToAppend);
    }
    
    public void WriteLine(string format, params object[] args)
    {
        GenerationEnvironment.Append(currentIndent);
        GenerationEnvironment.AppendFormat(format, args);
        GenerationEnvironment.AppendLine();
    }
    
    public class ToStringInstanceHelper
    {    
        private IFormatProvider formatProvider = System.Globalization.CultureInfo.InvariantCulture;
        
        public IFormatProvider FormatProvider
        {
            get => formatProvider;
            set => formatProvider = value ?? formatProvider;
        }
        
        public string ToStringWithCulture(object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (value is IConvertible convertible)
                return convertible.ToString(formatProvider);
            
            var method = value.GetType().GetMethod(nameof(ToString), [typeof(IFormatProvider)]);
            if (method is not null)
                return (string)method.Invoke(value, [formatProvider]);
            
            return value.ToString();
        }
    }
}
