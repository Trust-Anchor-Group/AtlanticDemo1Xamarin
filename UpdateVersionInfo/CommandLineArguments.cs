﻿using System;
using System.Collections.Generic;

namespace UpdateVersionInfo
{
    public class CommandLineArguments
    {
        private readonly OptionSet _options;
        public bool ShowHelp { get; private set; }
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int? Build { get; private set; }
        /*!!!
        public int? Revision { get; private set; }
        */
        public String VersionCsPath { get; private set; }
        public String AndroidManifestPath { get; private set; }
        public String TouchPListPath { get; private set; }

        private OptionSet Initialize()
        {
            OptionSet options = new()
            {
                {
                    "?", "Shows help/usage information.", h => ShowHelp = true
                },
                {
                    "v|major=", "A numeric major version number greater than zero.", (int v) => Major = v
                },
                {
                    "m|minor=", "A numeric minor number greater than zero.", (int v) => Minor = v
                },
                {
                    "b|build=", "A numeric build number greater than zero.", (int v) => Build = v
                },
                /*!!!
                {
                    "r|revision=", "A numeric revision number greater than zero.", (int v) => Revision = v
                },
                */
                {
                    "p|path=", "The path to a C# file to update with version information.", p => VersionCsPath = p
                },
                {
                    "a|androidManifest=", "The path to an android manifest file to update with version information.", p => AndroidManifestPath = p
                },
                {
                    "t|touchPlist=", "The path to an iOS plist file to update with version information.", p => TouchPListPath = p
                }
            };

            return options;
        }

        public CommandLineArguments(IEnumerable<String> args)
        {
            Major = 1;
            Minor = 0;
            _options = Initialize();
            _options.Parse(args);

        }

        public void WriteHelp(System.IO.TextWriter writer)
        {
            _options.WriteOptionDescriptions(writer);
        }
    }
}
