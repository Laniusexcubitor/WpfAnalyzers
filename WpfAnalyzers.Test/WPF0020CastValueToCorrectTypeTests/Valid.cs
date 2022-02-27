﻿namespace WpfAnalyzers.Test.WPF0020CastValueToCorrectTypeTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

public static class Valid
{
    private static readonly CallbackAnalyzer Analyzer = new();
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.WPF0020CastValueToCorrectType;

    [Test]
    public static void DependencyPropertyRegisterNoMetadata()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("new PropertyMetadata(OnBarChanged)")]
    [TestCase("new PropertyMetadata(new PropertyChangedCallback(OnBarChanged))")]
    [TestCase("new PropertyMetadata(default(int), OnBarChanged)")]
    [TestCase("new PropertyMetadata(default(int), new PropertyChangedCallback(OnBarChanged))")]
    [TestCase("new PropertyMetadata((o, e) => { })")]
    [TestCase("new FrameworkPropertyMetadata((o, e) => { })")]
    [TestCase("new FrameworkPropertyMetadata(OnBarChanged)")]
    [TestCase("new FrameworkPropertyMetadata(OnBarChanged, CoerceBar)")]
    public static void DependencyPropertyRegisterWithMetadata(string metadata)
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int), OnBarChanged));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
            var oldValue = (int)e.OldValue;
            var newValue = (int)e.NewValue;
        }

        private static object CoerceBar(DependencyObject d, object baseValue)
        {
            return baseValue;
        }
    }
}".AssertReplace("new PropertyMetadata(default(int), OnBarChanged)", metadata);

        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("int",                            "int")]
    [TestCase("System.IO.Stream",               "System.IDisposable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IEnumerable")]
    [TestCase("System.Collections.IList",       "System.Collections.IEnumerable")]
    public static void DependencyPropertyRegisterWithAllCallbacksDirectCast(string type, string toType)
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int), OnValueChanged, CoerceValue),
            ValidateValue);

        public int Value
        {
            get { return (int)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
            var oldValue = (string)e.OldValue;
            var newValue = (string)e.NewValue;
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var value = (string)baseValue;
            return value;
        }

        private static bool ValidateValue(object baseValue)
        {
            var value = (string)baseValue;
            return !object.Equals(value, null);
        }
    }
}".AssertReplace("int", type)
  .AssertReplace("string", toType);

        RoslynAssert.Valid(Analyzer, Descriptor, code);
    }

    [TestCase("object",                         "string")]
    [TestCase("object",                         "System.Collections.IEnumerable")]
    [TestCase("bool",                           "bool?")]
    [TestCase("System.IO.Stream",               "System.IDisposable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IEnumerable")]
    [TestCase("System.Collections.IList",       "System.Collections.IEnumerable")]
    public static void DependencyPropertyRegisterWithAllCallbacksAsCast(string type, string asType)
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int), OnValueChanged, CoerceValue),
            ValidateValue);

        public int Value
        {
            get { return (int)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as FooControl;
            var oldValue = e.OldValue as string;
            var newValue = e.NewValue as string;
        }

        private static object? CoerceValue(DependencyObject d, object? baseValue)
        {
            var value = baseValue as string;
            return value;
        }

        private static bool ValidateValue(object? baseValue)
        {
            var value = baseValue as string;
            return value != null;
        }
    }
}".AssertReplace("int", type)
  .AssertReplace("string", asType);

        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("object",                         "string")]
    [TestCase("object",                         "System.Collections.IEnumerable")]
    [TestCase("bool",                           "bool")]
    [TestCase("System.IO.Stream",               "System.IDisposable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IEnumerable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IList")]
    public static void DependencyPropertyRegisterWithAllCallbacksIsPatterns(string type, string isType)
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int), OnValueChanged, CoerceValue),
            ValidateValue);

        public int Value
        {
            get { return (int)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FooControl control)
            {
                if (e.OldValue is string oldValue)
                {
                }
                if (e.NewValue is string newValue)
                {
                }
            }
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            if (d is FooControl control)
            {
                if (baseValue is string text)
                {
                }
            }

            return baseValue;
        }

        private static bool ValidateValue(object baseValue)
        {
            if (baseValue is string text)
            {
                return !object.Equals(text, null);
            }

            return false;
        }
    }
}".AssertReplace("int", type)
  .AssertReplace("string", isType);

        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("object",                         "string")]
    [TestCase("object",                         "System.Collections.IEnumerable")]
    [TestCase("bool",                           "bool")]
    [TestCase("System.IO.Stream",               "System.IDisposable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IEnumerable")]
    [TestCase("System.Collections.IEnumerable", "System.Collections.IList")]
    public static void DependencyPropertyRegisterWithAllCallbacksSwitchPatterns(string type, string caseType)
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int), OnValueChanged, CoerceValue),
            ValidateValue);

        public int Value
        {
            get { return (int)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }


        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            switch (d)
            {
                case FooControl control:
                    {
                        switch (e.OldValue)
                        {
                            case string oldText:
                                break;
                        }

                        switch (e.NewValue)
                        {
                            case string newText:
                                break;
                        }
                    }
                    break;
            }
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            switch (d)
            {
                case FooControl control:
                {
                    switch (baseValue)
                    {
                        case string oldText:
                            break;
                    }
                }
                    break;
            }

            return baseValue;
        }

        private static bool ValidateValue(object baseValue)
        {
            switch (baseValue)
            {
                case string text:
                    return !object.Equals(text, null);
                default:
                    return false;
            }
        }
    }
}".AssertReplace("int", type)
  .AssertReplace("string", caseType);

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyRegisterReadOnly()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(1.0, OnValueChanged));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
            var oldValue = (double)e.OldValue;
            var newValue = (double)e.NewValue;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyRegisterAttached()
    {
        var code = @"
namespace N
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int), OnBarChanged));

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarProperty, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FrameworkElement)d;
            var oldValue = (int)e.OldValue;
            var newValue = (int)e.NewValue;
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyRegisterAttachedReadOnly()
    {
        var code = @"
namespace N
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int), OnBarChanged));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarPropertyKey, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FrameworkElement)d;
            var oldValue = (int)e.OldValue;
            var newValue = (int)e.NewValue;
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyOverrideMetadata()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : UserControl
    {
        static FooControl()
        {
            BackgroundProperty.OverrideMetadata(typeof(FooControl),
                new FrameworkPropertyMetadata(null, OnBackgroundChanged));
        }

        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
            var oldValue = (System.Windows.Media.Brush)e.OldValue;
            var newValue = (System.Windows.Media.Brush)e.NewValue;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyAddOwner()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Documents;

    public class FooControl : FrameworkElement
    {
        static FooControl()
        {
            TextElement.FontSizeProperty.AddOwner(typeof(FooControl), new PropertyMetadata(12.0, OnFontSizeChanged));
        }

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FooControl)d;
            var oldValue = (double)e.OldValue;
            var newValue = (double)e.NewValue;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void EnumIssue211()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        /// <summary>Identifies the <see cref=""FooEnum""/> dependency property.</summary>
        public static readonly DependencyProperty FooEnumProperty = DependencyProperty.Register(
            nameof(FooEnum),
            typeof(FooEnum),
            typeof(FooControl),
            new PropertyMetadata(FooEnum.Bar));

        public FooEnum FooEnum
        {
            get => (FooEnum) this.GetValue(FooEnumProperty);
            set => this.SetValue(FooEnumProperty, value);
        }
    }
}";
        var enumCode = @"namespace N
{
    public enum FooEnum
    {
        Bar,
        Baz
    }
}";
        RoslynAssert.Valid(Analyzer, code, enumCode);
    }

    [Test]
    public static void EnumAddOwnerIssue211()
    {
        var fooCode = @"
namespace N
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty FooEnumProperty = DependencyProperty.RegisterAttached(
            ""FooEnum"",
            typeof(FooEnum),
            typeof(Foo),
            new FrameworkPropertyMetadata(FooEnum.Baz, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Helper for setting <see cref=""FooEnumProperty""/> on <paramref name=""element""/>.</summary>
        /// <param name=""element""><see cref=""DependencyObject""/> to set <see cref=""FooEnumProperty""/> on.</param>
        /// <param name=""value"">FooEnum property value.</param>
        public static void SetFooEnum(DependencyObject element, FooEnum value)
        {
            element.SetValue(FooEnumProperty, value);
        }

        /// <summary>Helper for getting <see cref=""FooEnumProperty""/> from <paramref name=""element""/>.</summary>
        /// <param name=""element""><see cref=""DependencyObject""/> to read <see cref=""FooEnumProperty""/> from.</param>
        /// <returns>FooEnum property value.</returns>
        public static FooEnum GetFooEnum(DependencyObject element)
        {
            return (FooEnum)element.GetValue(FooEnumProperty);
        }
    }
}";
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        /// <summary>Identifies the <see cref=""FooEnum""/> dependency property.</summary>
        public static readonly DependencyProperty FooEnumProperty = Foo.FooEnumProperty.AddOwner(
            typeof(FooControl),
            new FrameworkPropertyMetadata(
                FooEnum.Bar,
                FrameworkPropertyMetadataOptions.Inherits));

        public FooEnum FooEnum
        {
            get => (FooEnum) this.GetValue(FooEnumProperty);
            set => this.SetValue(FooEnumProperty, value);
        }
    }
}";
        var enumCode = @"namespace N
{
    public enum FooEnum
    {
        Bar,
        Baz
    }
}";
        RoslynAssert.Valid(Analyzer, fooCode, code, enumCode);
    }
}