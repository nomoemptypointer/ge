using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Engine.Editor
{
    public class CommandLineOptions
    {
        public bool PreferOpenGL { get; }
        public string Project { get; }
        public string Scene { get; }
        public AudioEnginePreference? AudioPreference { get; }

        public CommandLineOptions(string[] args)
        {
            var openGlOption = new Option<bool>(
                "--opengl",
                description: "Prefer using the OpenGL rendering backend.")
            {
                Arity = ArgumentArity.Zero
            };

            var projectOption = new Option<string>(
                aliases: new[] { "--project", "-p" },
                description: "Specifies the project to open.");

            var sceneOption = new Option<string>(
                aliases: new[] { "--scene", "-s" },
                description: "Specifies the scene to open.");

            var audioOption = new Option<AudioEnginePreference?>(
                "--audio",
                parseArgument: result =>
                {
                    if (Enum.TryParse<AudioEnginePreference>(
                        result.Tokens[0].Value,
                        true,
                        out var pref))
                    {
                        return pref;
                    }

                    return null;
                },
                description: "Specifies the audio engine to use.");

            var root = new RootCommand("Editor")
            {
                openGlOption,
                projectOption,
                sceneOption,
                audioOption
            };

            ParseResult parsed = root.Parse(args);

            PreferOpenGL =
                parsed.GetValueForOption(openGlOption)
                || EditorPreferences.Instance.PreferOpenGL;

            Project = parsed.GetValueForOption(projectOption);
            Scene = parsed.GetValueForOption(sceneOption);
            AudioPreference = parsed.GetValueForOption(audioOption);
        }

        public CommandLineOptions()
        {
            PreferOpenGL = EditorPreferences.Instance.PreferOpenGL;
        }

        public enum AudioEnginePreference
        {
            OpenAL,
            None
        }
    }
}
