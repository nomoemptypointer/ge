using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Engine
{
    public class EngineLaunchOptions
    {
        public bool PreferOpenGL { get; }
        public AudioEnginePreference? AudioPreference { get; }

        public EngineLaunchOptions(string[] args)
        {
            var openGlOption = new Option<bool>(
                name: "--opengl",
                description: "Prefer using the OpenGL rendering backend."
            );

            var audioOption = new Option<AudioEnginePreference?>(
                name: "--audio",
                parseArgument: result =>
                {
                    if (Enum.TryParse<AudioEnginePreference>(
                        result.Tokens[0].Value,
                        true,
                        out var pref))
                    {
                        return pref;
                    }

                    return AudioEnginePreference.Default;
                },
                description: "Select audio backend."
            );

            var root = new RootCommand("Editor")
            {
                openGlOption,
                audioOption
            };

            ParseResult parsed = root.Parse(args);

            PreferOpenGL = parsed.GetValueForOption(openGlOption);
            AudioPreference = parsed.GetValueForOption(audioOption);
        }

        public enum AudioEnginePreference
        {
            Default,
            OpenAL,
            None
        }
    }
}
