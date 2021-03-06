﻿namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using TAUtil.Hpi;

    public abstract class AbstractHpiLoader<T>
    {
        protected AbstractHpiLoader()
        {
            this.Records = new List<T>();
            this.FileErrors = new List<HpiInnerFileErrorInfo>();
            this.HpiErrors = new List<HpiErrorInfo>();
        }

        public List<T> Records { get; }

        public List<HpiInnerFileErrorInfo> FileErrors { get; }

        public List<HpiErrorInfo> HpiErrors { get; }

        public bool LoadFiles()
        {
            return this.LoadFiles(i => { }, () => false);
        }

        public bool LoadFiles(Action<int> progressCallback, Func<bool> cancelCallback)
        {
            var hpis = LoadingUtils.EnumerateSearchHpis().ToList();
            var fileCount = 0;

            foreach (var file in hpis)
            {
                if (cancelCallback())
                {
                    return false;
                }

                this.LoadHpi(file);

                var progress = (++fileCount * 100) / hpis.Count;
                progressCallback(progress);
            }

            return true;
        }

        public LoadResult<T> GetResult()
        {
            return new LoadResult<T>
                {
                    Records = this.Records,
                    Errors = this.HpiErrors,
                    FileErrors = this.FileErrors,
                };
        }

        protected abstract IEnumerable<HpiArchive.FileInfo> EnumerateFiles(HpiArchive r);

        protected abstract void LoadFile(HpiArchive archive, HpiArchive.FileInfo file);

        private void OnHpiError(string hpi, Exception e)
        {
            this.HpiErrors.Add(new HpiErrorInfo { HpiPath = hpi, Error = e });
        }

        private void OnError(string hpi, string path, Exception e)
        {
            this.FileErrors.Add(new HpiInnerFileErrorInfo { HpiPath = hpi, FeaturePath = path, Error = e });
        }

        private void LoadHpi(string hpiFile)
        {
            try
            {
                using (var s = new HpiArchive(hpiFile))
                {
                    foreach (var file in this.EnumerateFiles(s))
                    {
                        try
                        {
                            this.LoadFile(s, file);
                        }
                        catch (Exception e)
                        {
                            this.OnError(hpiFile, file.FullPath, e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.OnHpiError(hpiFile, e);
            }
        }
    }
}
