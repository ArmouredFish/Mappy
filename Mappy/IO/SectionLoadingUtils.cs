﻿namespace Mappy.IO
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;

    using Mappy.Data;
    using Mappy.Palette;

    using TAUtil.Hpi;
    using TAUtil.Sct;

    public class SectionLoadingUtils
    {
        public static BackgroundWorker LoadSectionsBackgroundWorker()
        {
            var bg = new BackgroundWorker();
            bg.DoWork += delegate(object sender, DoWorkEventArgs args)
                {
                    var p = (IPalette)args.Argument;
                    args.Result = LoadSections(p);
                };

            bg.WorkerSupportsCancellation = false;
            bg.WorkerReportsProgress = false;

            return bg;
        }

        public static IList<Section> LoadSections(IPalette palette)
        {
            IList<Section> sections = new List<Section>();
            int i = 0;
            foreach (string file in LoadingUtils.EnumerateSearchHpis())
            {
                foreach (Section s in LoadSectionsFromHapi(file, palette))
                {
                    s.Id = i++;
                    sections.Add(s);
                }
            }

            return sections;
        }

        private static IEnumerable<Section> LoadSectionsFromHapi(string filename, IPalette palette)
        {
            var factory = new SectionFactory(palette);

            using (HpiReader h = new HpiReader(filename))
            {
                foreach (string sect in h.GetFilesRecursive("sections").Select(x => x.Name))
                {
                    using (var s = new SctReader(h.ReadFile(sect)))
                    {
                        Section section = new Section(filename, sect);
                        section.Name = Path.GetFileNameWithoutExtension(sect);
                        section.Minimap = factory.MinimapFromSct(s);
                        section.DataWidth = s.DataWidth;
                        section.DataHeight = s.DataHeight;

                        string[] directories = Path.GetDirectoryName(sect).Split(Path.DirectorySeparatorChar);

                        section.World = directories[1];
                        section.Category = directories[2];

                        yield return section;
                    }
                }
            }
        }
    }
}
