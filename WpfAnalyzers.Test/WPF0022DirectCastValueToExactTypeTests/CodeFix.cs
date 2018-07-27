namespace WpfAnalyzers.Test.WPF0022DirectCastValueToExactTypeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CallbackAnalyzer();
        private static readonly CodeFixProvider Fix = new FixCastCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("WPF0022");

        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var expectedDiagnostic = ExpectedDiagnostic.Create(
                "WPF0022",
                "Value is of type string.");
            AnalyzerAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [TestCase("new PropertyMetadata(1, OnValueChanged)")]
        [TestCase("new PropertyMetadata(1, new PropertyChangedCallback(OnValueChanged))")]
        public void DependencyPropertyRegisterPropertyChangedCallbackMethodGroup(string metadata)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            testCode = testCode.AssertReplace("new PropertyMetadata(1, OnValueChanged)", metadata);
            fixedCode = fixedCode.AssertReplace("new PropertyMetadata(1, OnValueChanged)", metadata);

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("OnValueChanged")]
        [TestCase("new PropertyChangedCallback(OnValueChanged)")]
        public void DependencyPropertyRegisterPropertyChangedCallbackMethodGroupCallingInstanceMethod(string callback)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FooControl)d).OnValueChanged(e.OldValue, (↓System.Collections.IEnumerable)e.NewValue);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FooControl)d).OnValueChanged(e.OldValue, (string)e.NewValue);
        }
    }
}";
            testCode = testCode.AssertReplace("new PropertyMetadata(1, OnValueChanged)", $"new PropertyMetadata(1, {callback})");
            fixedCode = fixedCode.AssertReplace("new PropertyMetadata(1, OnValueChanged)", $"new PropertyMetadata(1, {callback})");
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("(d, e) => OnValueChanged(d, e)")]
        [TestCase("new PropertyChangedCallback((d, e) => OnValueChanged(d, e))")]
        public void DependencyPropertyRegisterPropertyChangedCallbackLambda(string lambda)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(
                1,
                (d, e) => OnValueChanged(d, e)));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(
                1,
                (d, e) => OnValueChanged(d, e)));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            testCode = testCode.AssertReplace("(d, e) => OnValueChanged(d, e)", lambda);
            fixedCode = fixedCode.AssertReplace("(d, e) => OnValueChanged(d, e)", lambda);
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("(d, e) => ((FooControl)d).OnValueChanged((↓↓System.Collections.IEnumerable)e.OldValue, (string)e.NewValue)")]
        [TestCase("(d, e) => ((FooControl)d).OnValueChanged((string)e.OldValue, (↓↓System.Collections.IEnumerable)e.NewValue)")]
        public void DependencyPropertyRegisterPropertyChangedCallbackLambdaCallingInstanceMethod(string lambda)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(
                default(string),
                (d, e) => ((FooControl)d).OnValueChanged((string)e.OldValue, (string)e.NewValue)));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(
                default(string),
                (d, e) => ((FooControl)d).OnValueChanged((string)e.OldValue, (string)e.NewValue)));

        public string Value
        {
            get => (string)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
        }
    }
}";
            testCode = testCode.AssertReplace("(d, e) => ((FooControl)d).OnValueChanged((string)e.OldValue, (string)e.NewValue)", lambda);
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("new PropertyMetadata(1, OnValueChanged, CoerceValue)")]
        [TestCase("new PropertyMetadata(1, new PropertyChangedCallback(OnValueChanged), new CoerceValueCallback(CoerceValue))")]
        public void DependencyPropertyRegisterCoerceValueCallback(string metadata)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged, CoerceValue));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
        }

        private static object CoerceValue(DependencyObject d, object basevalue)
        {
            return (↓System.Collections.IEnumerable)basevalue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged, CoerceValue));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
        }

        private static object CoerceValue(DependencyObject d, object basevalue)
        {
            return (string)basevalue;
        }
    }
}";
            testCode = testCode.AssertReplace("new PropertyMetadata(1, OnValueChanged, CoerceValue)", metadata);
            fixedCode = fixedCode.AssertReplace("new PropertyMetadata(1, OnValueChanged, CoerceValue)", metadata);
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("ValidateValue")]
        [TestCase("new ValidateValueCallback(ValidateValue)")]
        public void DependencyPropertyRegisterValidateValue(string validateValue)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged, CoerceValue),
            ValidateValue);

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
        }

        private static object CoerceValue(DependencyObject d, object basevalue)
        {
            return (string)basevalue;
        }

        private static bool ValidateValue(object basevalue)
        {
            return ((↓System.Collections.IEnumerable)basevalue) != null;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1, OnValueChanged, CoerceValue),
            ValidateValue);

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
        }

        private static object CoerceValue(DependencyObject d, object basevalue)
        {
            return (string)basevalue;
        }

        private static bool ValidateValue(object basevalue)
        {
            return ((string)basevalue) != null;
        }
    }
}";
            testCode = testCode.AssertReplace("ValidateValue);", validateValue + ");");
            fixedCode = fixedCode.AssertReplace("ValidateValue);", validateValue + ");");
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("(↓System.Collections.IEnumerable)e.NewValue", "(string)e.NewValue")]
        [TestCase("(↓System.Collections.IEnumerable)e.OldValue", "(string)e.OldValue")]
        public void DependencyPropertyRegisterCast(string cast, string expectedCast)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(default(string), OnValueChanged));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(default(string), OnValueChanged));

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            testCode = testCode.AssertReplace("(↓System.Collections.IEnumerable)e.NewValue", cast);
            fixedCode = fixedCode.AssertReplace("(string)e.NewValue", expectedCast);

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void DependencyPropertyRegisterReadOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1.0, OnValueChanged));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(string),
            typeof(FooControl),
            new PropertyMetadata(1.0, OnValueChanged));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public string Value
        {
            get { return (string)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void DependencyPropertyRegisterAttached()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(string),
            typeof(Foo),
            new PropertyMetadata(1, OnValueChanged));

        public static void SetBar(this FrameworkElement element, string value) => element.SetValue(BarProperty, value);

        public static string GetBar(this FrameworkElement element) => (string)element.GetValue(BarProperty);

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(string),
            typeof(Foo),
            new PropertyMetadata(1, OnValueChanged));

        public static void SetBar(this FrameworkElement element, string value) => element.SetValue(BarProperty, value);

        public static string GetBar(this FrameworkElement element) => (string)element.GetValue(BarProperty);

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void DependencyPropertyRegisterAttachedReadOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(string),
            typeof(Foo),
            new PropertyMetadata(default(string), OnValueChanged));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, string value) => element.SetValue(BarPropertyKey, value);

        public static string GetBar(this FrameworkElement element) => (string)element.GetValue(BarProperty);

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(string),
            typeof(Foo),
            new PropertyMetadata(default(string), OnValueChanged));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, string value) => element.SetValue(BarPropertyKey, value);

        public static string GetBar(this FrameworkElement element) => (string)element.GetValue(BarProperty);

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void DependencyPropertyAddOwner()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(string), 
            typeof(Foo), 
            new FrameworkPropertyMetadata(
                default(string), 
                FrameworkPropertyMetadataOptions.Inherits));

        public static void SetBar(DependencyObject element, string value)
        {
            element.SetValue(BarProperty, value);
        }

        public static string GetBar(DependencyObject element)
        {
            return (string) element.GetValue(BarProperty);
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = Foo.BarProperty.AddOwner(
            typeof(FooControl), 
            new FrameworkPropertyMetadata(
                default(string), 
                OnBarChanged));

        public string Bar
        {
            get => (string)this.GetValue(BarProperty);
            set => this.SetValue(BarProperty, value);
        }

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (↓System.Collections.IEnumerable)e.NewValue;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = Foo.BarProperty.AddOwner(
            typeof(FooControl), 
            new FrameworkPropertyMetadata(
                default(string), 
                OnBarChanged));

        public string Bar
        {
            get => (string)this.GetValue(BarProperty);
            set => this.SetValue(BarProperty, value);
        }

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (string)e.NewValue;
        }
    }
}";

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, testCode }, fixedCode);
        }
    }
}
