using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Uses the DotNetZip library to create and extract zip files.
    /// </summary>
    internal static class DotNetZip
    {

        public delegate void DelegateWriteEntry(string entryName, Stream stream);

        public delegate void DelegateExtractEntry(string entryName, out Stream stream, out bool closeStream);

        public delegate void ProgressCallback(int entriesProcessed, int entriesTotal);

        public static void WriteZipArchive(
            Assembly dotNetZipAssembly,
            Stream outStream,
            string[] entryNames,
            DelegateWriteEntry entryWriter,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || outStream == null)
            {
                throw new ArgumentNullException();
            }

            entryNames = (string[])(entryNames ?? new string[] { }).Clone();

            if (entryNames.Length > 0 && entryWriter == null)
            {
                throw new ArgumentNullException();
            }

            if (entryNames.Contains(null))
            {
                throw new ArgumentNullException();
            }

            Type zipFileType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipFile");
            ConstructorInfo zipFileConstructor = ReflectionHelper.GetConstructor(zipFileType, new Type[] { });
            Type writeDelegateType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.WriteDelegate");
            MethodInfo addEntryMethod = ReflectionHelper.GetMethod(zipFileType,
                "AddEntry", new Type[] { typeof(string), writeDelegateType });
            MethodInfo saveMethod = ReflectionHelper.GetMethod(zipFileType, "Save", new Type[] { typeof(Stream) });
            EventInfo saveProgressEvent = ReflectionHelper.GetEvent(zipFileType, "SaveProgress");
            Type saveProgressEventArgsType =
                ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.SaveProgressEventArgs");
            PropertyInfo entriesSavedProperty =
                ReflectionHelper.GetProperty(saveProgressEventArgsType, "EntriesSaved");
            PropertyInfo entriesTotalProperty =
                ReflectionHelper.GetProperty(saveProgressEventArgsType, "EntriesTotal");
            PropertyInfo eventTypeProperty = ReflectionHelper.GetProperty(saveProgressEventArgsType, "EventType");
            Type zipProgressEventTypeEnum =
                ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipProgressEventType");
            object afterWriteEntryEnumValue =
                ReflectionHelper.GetEnumValue(zipProgressEventTypeEnum, "Saving_AfterWriteEntry");

            Delegate writeDelegate = Delegate.CreateDelegate(
                writeDelegateType,
                new WriteDelegateImpl(entryWriter),
                typeof(WriteDelegateImpl).GetMethod("WriteDelegate"));

            Delegate saveProgressDelegate = Delegate.CreateDelegate(
                saveProgressEvent.EventHandlerType,
                new SaveProgressDelegateImpl(progressCallback, entriesSavedProperty, entriesTotalProperty,
                    eventTypeProperty, afterWriteEntryEnumValue),
                typeof(SaveProgressDelegateImpl).GetMethod("OnEvent"));

            IDisposable zipFile = (IDisposable)zipFileConstructor.Invoke(null);
            using (zipFile)
            {
                foreach (string entryName in entryNames)
                {
                    addEntryMethod.Invoke(zipFile, new object[] { entryName, writeDelegate });
                }
                if (progressCallback != null)
                {
                    saveProgressEvent.AddEventHandler(zipFile, saveProgressDelegate);
                }
                saveMethod.Invoke(zipFile, new object[] { outStream });
            }

        }

        public static void ExtractZipArchive(
            Assembly dotNetZipAssembly,
            Stream inStream,
            string[] entryNames,
            DelegateExtractEntry entryExtractor,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || inStream == null)
            {
                throw new ArgumentNullException();
            }

            entryNames = (string[])(entryNames ?? new string[] { }).Clone();

            if (entryNames.Contains(null))
            {
                throw new ArgumentNullException();
            }

            if (entryNames.Length > 0 && entryExtractor == null)
            {
                throw new ArgumentNullException();
            }

            Type zipFileType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipFile");
            Type zipEntryType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipEntry");
            MethodInfo readMethod = ReflectionHelper.GetMethod(zipFileType, "Read", new Type[] { typeof(Stream) });
            PropertyInfo entriesProperty = ReflectionHelper.GetProperty(zipFileType, "Entries");
            PropertyInfo fileNameProperty = ReflectionHelper.GetProperty(zipEntryType, "FileName");
            MethodInfo extractMethod = ReflectionHelper.GetMethod(
                zipEntryType, "Extract", new Type[] { typeof(Stream) });

            HashSet<string> entryNameSet = new HashSet<string>(entryNames);
            inStream = new DotNetZipCompatibilityReadStream(inStream);

            IDisposable zipEntry = (IDisposable)readMethod.Invoke(null, new object[] { inStream });
            using (zipEntry)
            {
                IEnumerable entries = (IEnumerable)entriesProperty.GetValue(zipEntry);

                HashSet<string> zipEntryNames = new HashSet<string>(
                    entries.Cast<object>().Select(x => (string)fileNameProperty.GetValue(x)));
                foreach (string entryName in entryNames)
                {
                    if (!zipEntryNames.Contains(entryName))
                    {
                        throw new Exception("Entry name not found in archive: " + entryName);
                    }
                    if (entryName.EndsWith("/"))
                    {
                        throw new Exception("Entry name cannot be a folder: " + entryName);
                    }
                }

                IEnumerator entryEnumerator = entries.GetEnumerator();
                int entriesProcessed = 0;
                int entriesTotal = entryNames.Length;
                while (entryEnumerator.MoveNext())
                {
                    object entry = entryEnumerator.Current;
                    string entryName = (string)fileNameProperty.GetValue(entry);
                    if (entryNameSet.Contains(entryName))
                    {
                        Stream stream;
                        bool closeStream;
                        entryExtractor(entryName, out stream, out closeStream);
                        if (stream == null) { throw new ArgumentNullException(); }
                        try
                        {
                            extractMethod.Invoke(entry, new object[] { stream });
                        }
                        finally
                        {
                            if (closeStream)
                            {
                                stream.Close();
                            }
                        }
                        if (progressCallback != null)
                        {
                            progressCallback(entriesProcessed++, entriesTotal);
                        }
                    }
                }
            }
        }

        public static string[] ReadZipArchiveEntryNames(Assembly dotNetZipAssembly, Stream inStream)
        {
            if (dotNetZipAssembly == null || inStream == null)
            {
                throw new ArgumentNullException();
            }

            Type zipFileType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipFile");
            Type zipEntryType = ReflectionHelper.GetType(dotNetZipAssembly, "Ionic.Zip.ZipEntry");
            MethodInfo readMethod = ReflectionHelper.GetMethod(zipFileType, "Read", new Type[] { typeof(Stream) });
            PropertyInfo entriesProperty = ReflectionHelper.GetProperty(zipFileType, "Entries");
            PropertyInfo fileNameProperty = ReflectionHelper.GetProperty(zipEntryType, "FileName");
            MethodInfo extractMethod = ReflectionHelper.GetMethod(
                zipEntryType, "Extract", new Type[] { typeof(Stream) });

            inStream = new DotNetZipCompatibilityReadStream(inStream);
            IDisposable zipEntry = (IDisposable)readMethod.Invoke(null, new object[] { inStream });
            List<string> entryNames = new List<string>();
            using (zipEntry)
            {
                IEnumerable entries = (IEnumerable)entriesProperty.GetValue(zipEntry);
                IEnumerator entryEnumerator = entries.GetEnumerator();
                while (entryEnumerator.MoveNext())
                {
                    object entry = entryEnumerator.Current;
                    string entryName = (string)fileNameProperty.GetValue(entry);
                    entryNames.Add(entryName);
                }
            }
            return entryNames.ToArray();
        }

        private class WriteDelegateImpl
        {

            private readonly DelegateWriteEntry entryWriter;

            public WriteDelegateImpl(DelegateWriteEntry entryWriter)
            {
                this.entryWriter = entryWriter;
            }

            public void WriteDelegate(string entryName, Stream stream)
            {
                if (!entryName.EndsWith("/"))
                {
                    entryWriter(entryName, stream);
                }
            }

        }

        private class SaveProgressDelegateImpl
        {

            private readonly ProgressCallback progressCallback;
            private readonly PropertyInfo entriesSavedProperty;
            private readonly PropertyInfo entriesTotalProperty;
            private readonly PropertyInfo eventTypeProperty;
            private readonly object afterWriteEntryEnumValue;

            public SaveProgressDelegateImpl(
                ProgressCallback progressCallback,
                PropertyInfo entriesSavedProperty,
                PropertyInfo entriesTotalProperty,
                PropertyInfo eventTypeProperty,
                object afterWriteEntryEnumValue)
            {
                this.progressCallback = progressCallback;
                this.entriesSavedProperty = entriesSavedProperty;
                this.entriesTotalProperty = entriesTotalProperty;
                this.eventTypeProperty = eventTypeProperty;
                this.afterWriteEntryEnumValue = afterWriteEntryEnumValue;
            }

            public void OnEvent(object sender, object e)
            {
                object eventType = eventTypeProperty.GetValue(e);
                if (afterWriteEntryEnumValue.Equals(eventType))
                {
                    int entriesSaved = (int)entriesSavedProperty.GetValue(e);
                    int entriesTotal = (int)entriesTotalProperty.GetValue(e);
                    progressCallback(entriesSaved, entriesTotal);
                }
            }

        }

        /*
         * On Read() calls, fill the entire buffer as possible.
         * Required because DotNetZip is poorly written, and has errors otherwise...
         */
        private class DotNetZipCompatibilityReadStream : Stream
        {

            private readonly Stream stream;

            public DotNetZipCompatibilityReadStream(Stream stream)
            {
                this.stream = stream;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = 0;
                while (true)
                {
                    int r = stream.Read(buffer, offset + bytesRead, count - bytesRead);
                    if (r == 0 || bytesRead == count)
                    {
                        break;
                    }
                    bytesRead += r;
                }
                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return stream.Seek(offset, origin);
            }

            public override long Length
            {
                get
                {
                    return stream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return stream.Position;
                }
                set
                {
                    stream.Position = value;
                }
            }

            protected override void Dispose(bool disposing)
            {

            }

            public override void Flush()
            {

            }

            public override void Write(byte[] buffer, int offset, int count) {
                throw new NotSupportedException(); }
            public override bool CanRead { get { return stream.CanRead; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return stream.CanSeek; } }
            public override void SetLength(long value) { throw new NotSupportedException(); }

        }

    }
}