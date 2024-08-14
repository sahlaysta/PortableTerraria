using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sahlaysta.PortableTerrariaCommon
{
    internal static class DotNetZip
    {

        public delegate void DelegateWriteEntry(string entryName, Stream stream);

        public delegate void ProgressCallback(int nEntriesProcessed, int nEntriesTotal);

        public static void WriteZipArchive(
            Assembly dotNetZipAssembly,
            Stream outStream,
            string[] entryNames,
            DelegateWriteEntry entryWriter,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || outStream == null)
            {
                throw new ArgumentException("Null");
            }

            entryNames = (string[])(entryNames ?? new string[] { }).Clone();

            if (entryNames.Length > 0 && entryWriter == null)
            {
                throw new ArgumentException("Null");
            }

            if (entryNames.Contains(null))
            {
                throw new ArgumentException("Null");
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

    }
}