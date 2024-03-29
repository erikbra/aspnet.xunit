﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.ConsoleClient;
using Xunit.Sdk;

public class CommandLineTests
{
    public class Filename
    {
        [Fact]
        public static void MultipleAssembliesDoesNotThrow()
        {
            var arguments = new[] { "assemblyName.dll", "assemblyName2.dll" };

            var result = TestableCommandLine.Parse(arguments);

            Assert.Collection(result.Project,
                a =>
                {
                    Assert.Equal(Path.GetFullPath("assemblyName.dll"), a.AssemblyFilename);
                    Assert.Null(a.ConfigFilename);
                    Assert.True(a.ShadowCopy);
                },
                a =>
                {
                    Assert.Equal(Path.GetFullPath("assemblyName2.dll"), a.AssemblyFilename);
                    Assert.Null(a.ConfigFilename);
                    Assert.True(a.ShadowCopy);
                }
            );
        }
    }

    public class MaxThreadsOption
    {
        [Fact]
        public static void DefaultValueIsZero()
        {
            var commandLine = TestableCommandLine.Parse("assemblyName.dll");

            Assert.Equal(0, commandLine.MaxParallelThreads);
        }

        [Fact]
        public static void MissingValue()
        {
            var ex = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-maxthreads"));

            Assert.Equal("missing argument for -maxthreads", ex.Message);
        }

        [Fact]
        public static void InvalidValue()
        {
            var ex = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-maxthreads", "abc"));

            Assert.Equal("incorrect argument value for -maxthreads", ex.Message);
        }

        [Fact]
        public static void SetsMaxParallelThreads()
        {
            var commandLine = TestableCommandLine.Parse("assemblyName.dll", "-maxthreads", "16");

            Assert.Equal(16, commandLine.MaxParallelThreads);
        }
    }

    public class NoShadowOption
    {
        [Fact]
        public static void NoShadowNotSetShadowCopyTrue()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            var assembly = Assert.Single(commandLine.Project.Assemblies);
            Assert.True(assembly.ShadowCopy);
        }

        [Fact]
        public static void NoShadowSetShadowCopyFalse()
        {
            var arguments = new[] { "assemblyName.dll", "-noshadow" };

            var commandLine = TestableCommandLine.Parse(arguments);

            var assembly = Assert.Single(commandLine.Project.Assemblies);
            Assert.False(assembly.ShadowCopy);
        }
    }

    public class WaitOption
    {
        [Fact]
        public static void WaitOptionNotPassedWaitFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.Wait);
        }

        [Fact]
        public static void WaitOptionWaitIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-wait" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.Wait);
        }

        [Fact]
        public static void WaitOptionIgnoreCaseWaitIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-wAiT" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.Wait);
        }
    }

    public class TeamCityArgument
    {
        [Fact, TeamCityEnvironmentRestore]
        public static void TeamCityOptionNotPassedTeamCityFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.TeamCity);
        }

        [Fact, TeamCityEnvironmentRestore(Value = "TeamCity")]
        public static void TeamCityOptionNotPassedEnvironmentSetTeamCityTrue()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.TeamCity);
        }

        [Fact, TeamCityEnvironmentRestore]
        public static void TeamCityOptionTeamCityTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-teamcity" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.TeamCity);
        }

        [Fact, TeamCityEnvironmentRestore]
        public static void TeamCityOptionIgnoreCaseTeamCityTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-tEaMcItY" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.TeamCity);
        }

        class TeamCityEnvironmentRestore : BeforeAfterTestAttribute
        {
            string originalValue;

            public string Value { get; set; }

            public override void Before(MethodInfo methodUnderTest)
            {
                originalValue = Environment.GetEnvironmentVariable("TEAMCITY_PROJECT_NAME");
                Environment.SetEnvironmentVariable("TEAMCITY_PROJECT_NAME", Value);
            }

            public override void After(MethodInfo methodUnderTest)
            {
                Environment.SetEnvironmentVariable("TEAMCITY_PROJECT_NAME", originalValue);
            }
        }
    }

    public class TraitArgument
    {
        [Fact]
        public static void TraitArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedTraits.Count);
        }

        [Fact]
        public static void SingleValidTraitArgument()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_SameName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar", "-trait", "foo=baz" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(2, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
            Assert.Contains("baz", commandLine.Project.Filters.IncludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_DifferentName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar", "-trait", "baz=biff" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["baz"].Count());
            Assert.Contains("biff", commandLine.Project.Filters.IncludedTraits["baz"]);
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-trait" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -trait", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingEquals()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foobar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "=bar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionNameMissingValue()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void TooManyEqualsSigns()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar=baz" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }
    }

    public class MinusTraitArgument
    {
        [Fact]
        public static void TraitArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.ExcludedTraits.Count);
        }

        [Fact]
        public static void SingleValidTraitArgument()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_SameName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar", "-notrait", "foo=baz" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(2, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
            Assert.Contains("baz", commandLine.Project.Filters.ExcludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_DifferentName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar", "-notrait", "baz=biff" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["baz"].Count());
            Assert.Contains("biff", commandLine.Project.Filters.ExcludedTraits["baz"]);
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -notrait", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingEquals()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foobar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "=bar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionNameMissingValue()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void TooManyEqualsSigns()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar=baz" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }
    }

    public class MethodArgument
    {
        [Fact]
        public static void MethodArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedMethods.Count);
        }

        [Fact]
        public static void SingleValidMethodArgument()
        {
            const string name = "Namespace.Class.Method1";

            var arguments = new[] { "assemblyName.dll", "-method", name };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedMethods.Count);
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name));
        }

        [Fact]
        public static void MultipleValidMethodArguments()
        {
            const string name1 = "Namespace.Class.Method1";
            const string name2 = "Namespace.Class.Method2";

            var arguments = new[] { "assemblyName.dll", "-method", name1, "-method", name2 };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedMethods.Count);
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name1));
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name2));
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-method" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -method", ex.Message);
        }
    }

    public class ClassArgument
    {
        [Fact]
        public static void ClassArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedMethods.Count);
        }

        [Fact]
        public static void SingleValidClassArgument()
        {
            const string name = "Namespace.Class";

            var arguments = new[] { "assemblyName.dll", "-class", name };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedClasses.Count);
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name));
        }

        [Fact]
        public static void MultipleValidClassArguments()
        {
            const string name1 = "Namespace.Class1";
            const string name2 = "Namespace.Class2";

            var arguments = new[] { "assemblyName.dll", "-class", name1, "-class", name2 };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedClasses.Count);
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name1));
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name2));
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-class" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -class", ex.Message);
        }
    }

    public class ParallelizationOptions
    {
        [Fact]
        public static void ParallelIsCollectionsOnlyByDefault()
        {
            var project = TestableCommandLine.Parse("assemblyName.dll");

            Assert.True(project.ParallelizeTestCollections);
        }

        [Fact]
        public static void FailsWithoutOptionOrWithIncorrectOptions()
        {
            var aex1 = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-parallel"));
            Assert.Equal("missing argument for -parallel", aex1.Message);

            var aex2 = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-parallel", "nonsense"));
            Assert.Equal("incorrect argument value for -parallel", aex2.Message);
        }

        [Theory]
        [InlineData("none", false)]
        [InlineData("collections", true)]
        [InlineData("all", true)]
        public static void ParallelCanBeTurnedOn(string parallelOption, bool expectedCollectionsParallelization)
        {
            var project = TestableCommandLine.Parse("assemblyName.dll", "-parallel", parallelOption);

            Assert.Equal(expectedCollectionsParallelization, project.ParallelizeTestCollections);
        }
    }

    public class Transform
    {
        [Fact]
        public static void OutputMissingFilename()
        {
            var arguments = new[] { "assemblyName.dll", "-xml" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing filename for -xml", ex.Message);
        }

        [Fact]
        public static void Output()
        {
            var arguments = new[] { "assemblyName.dll", "-xml", "foo.xml" };

            var commandLine = TestableCommandLine.Parse(arguments);

            var output = Assert.Single(commandLine.Project.Output);
            Assert.Equal("xml", output.Key);
            Assert.Equal("foo.xml", output.Value);
        }
    }
 
    public class DesignTimeSwitch
    {
        [Theory]
        [InlineData("-designtime")]
        [InlineData("--designtime")]
        public static void DesignTime(string arg)
        {
            var arguments = new[] { "assemblyName.dll", arg };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.DesignTime);
        }
    }

    public class ListSwitch
    {
        [Theory]
        [InlineData("-list")]
        [InlineData("--list")]
        public static void List(string arg)
        {
            var arguments = new[] { "assemblyName.dll", arg };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.List);
        }
    }

    public class TestArgument
    {
        [Fact]
        public static void TestUniqueNames()
        {
            var arguments = new[]
            {
                "assemblyName.dll",
                "-test",
                "foo",
                "--test",
                "bar",
                "--test",
                "baz",
            };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(3, commandLine.DesignTimeTestUniqueNames.Count);
            Assert.Contains("foo", commandLine.DesignTimeTestUniqueNames);
            Assert.Contains("bar", commandLine.DesignTimeTestUniqueNames);
            Assert.Contains("baz", commandLine.DesignTimeTestUniqueNames);
        }
    }

    class TestableCommandLine : CommandLine
    {
        private TestableCommandLine(params string[] arguments)
            : base(arguments)
        { }

        public new static TestableCommandLine Parse(params string[] arguments)
        {
            return new TestableCommandLine(arguments);
        }
    }
}
