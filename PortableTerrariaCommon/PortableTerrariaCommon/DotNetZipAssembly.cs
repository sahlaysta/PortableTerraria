using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    // DotNetZip assembly reflection class
    static class DotNetZipAssembly
    {
        //reflection ZipFile
        public class ZipFile : IEnumerable<ZipEntry>, IDisposable
        {
            //reflection enumerator
            class ZipEntryEnumerator : IEnumerator<ZipEntry>
            {
                //reflection constructor
                public ZipEntryEnumerator(IEnumerator enumerator)
                {
                    ie = enumerator;
                }

                //interface operations
                ZipEntry IEnumerator<ZipEntry>.Current => getCurrent();
                object IEnumerator.Current => getCurrent();
                void IDisposable.Dispose() => ((IDisposable)ie).Dispose();
                bool IEnumerator.MoveNext()
                {
                    bool result = ie.MoveNext();
                    if (result)
                        current = null;
                    return result;
                }
                void IEnumerator.Reset()
                {
                    ie.Reset();
                    current = null;
                }

                ZipEntry getCurrent()
                {
                    return current ?? (current = new ZipEntry(ie.Current));
                }

                readonly IEnumerator ie;
                ZipEntry current;
            }

            //reflection constructor
            public ZipFile() : this(
                (IDisposable)rZipFileConstr.Invoke(null)) { }
            ZipFile(IDisposable instance)
            {
                this.instance = instance;
                addSaveProgressEventHandler(invokeSaveProgress);
            }
            public static ZipFile Read(Stream arg0)
            {
                return new ZipFile(
                    (IDisposable)rRead.Invoke(null, new object[] { arg0 }));
            }

            //reflection event
            public event EventHandler<SaveProgressEventArgs> SaveProgress;

            //public reflection operations
            public int Count
            {
                get => (int)rCount.GetValue(instance);
            }
            public void AddFile(string arg0)
            {
                rAddFile.Invoke(instance, new object[] { arg0 });
            }
            public void AddFile(string arg0, string arg1)
            {
                rAddFile2.Invoke(instance, new object[] { arg0, arg1 });
            }
            public void AddDirectory(string arg0)
            {
                rAddDirectory.Invoke(instance, new object[] { arg0 });
            }
            public void AddDirectory(string arg0, string arg1)
            {
                rAddDirectory2.Invoke(instance, new object[] { arg0, arg1 });
            }
            public void Save(Stream arg0)
            {
                rSave.Invoke(instance, new object[] { arg0 });
            }
            public IEnumerator<ZipEntry> GetEnumerator()
            {
                return getEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return getEnumerator();
            }
            public void Dispose()
            {
                instance.Dispose();
            }

            //reflection SaveProgress event
            void addSaveProgressEventHandler(EventHandler eh)
            {
                var d = (Delegate)
                    rSaveProgressConstr.Invoke(new object[]
                    {
                        eh.Target,
                        eh.Method.MethodHandle.GetFunctionPointer()
                    });
                rSaveProgress.AddEventHandler(instance, d);
            }
            void invokeSaveProgress(object sender, EventArgs e)
            {
                var eh = SaveProgress;
                if (eh == null || eh.GetInvocationList().Length == 0)
                    return;

                eh.Invoke(sender, new SaveProgressEventArgs(e));
            }

            IEnumerator<ZipEntry> getEnumerator()
            {
                IEnumerator ie = (IEnumerator)
                    rEnumerator.Invoke(instance, null);
                return new ZipEntryEnumerator(ie);
            }

            readonly IDisposable instance;
        }

        //reflection ZipEntry
        public class ZipEntry
        {
            //reflection constructor
            internal ZipEntry(object instance)
            {
                this.instance = instance;
            }

            //public reflection operations
            public string FileName
            {
                get => (string)rFileName.GetValue(instance);
            }
            public void Extract(Stream arg0)
            {
                rExtract.Invoke(instance, new object[] { arg0 });
            }

            readonly object instance;
        }

        //reflection SaveProgressEventArgs
        public class SaveProgressEventArgs : EventArgs
        {
            //reflection constructor
            internal SaveProgressEventArgs(EventArgs e)
            {
                this.e = e;
            }

            //public reflection operations
            public bool Cancel
            {
                get => (bool)rCancel.GetValue(e);
                set => rCancel.SetValue(e, value);
            }
            public bool EventTypeIsSaving_AfterWriteEntry
            {
                get => rEventType.GetValue(e)
                    .Equals(rSaving_AfterWriteEntry);
            }
            public int EntriesSaved
            {
                get => (int)rEntriesSaved.GetValue(e);
            }
            public int EntriesTotal
            {
                get => (int)rEntriesTotal.GetValue(e);
            }

            readonly EventArgs e;
        }

        //assembly
        static readonly Assembly assembly =
            Assembly.Load(GuiHelper.GetResourceBytes(
                "DotNetZip.dll"));

        //reflection fields
        static readonly Type rZipFile =
            assembly.GetType(
                "Ionic.Zip.ZipFile");
        static readonly ConstructorInfo rZipFileConstr =
            rZipFile.GetConstructors()
                .First(c => c.GetParameters().Length == 0);
        static readonly PropertyInfo rCount =
            rZipFile.GetProperty(
                "Count");
        static readonly MethodInfo rRead =
            rZipFile.GetMethod(
                "Read",
                new Type[] { typeof(Stream) });
        static readonly MethodInfo rAddFile =
            rZipFile.GetMethod(
                "AddFile",
                new Type[] { typeof(string) });
        static readonly MethodInfo rAddFile2 =
            rZipFile.GetMethod(
                "AddFile",
                new Type[] { typeof(string), typeof(string) });
        static readonly MethodInfo rAddDirectory =
            rZipFile.GetMethod(
                "AddDirectory",
                new Type[] { typeof(string) });
        static readonly MethodInfo rAddDirectory2 =
            rZipFile.GetMethod(
                "AddDirectory",
                new Type[] { typeof(string), typeof(string) });
        static readonly MethodInfo rSave =
            rZipFile.GetMethod(
                "Save",
                new Type[] { typeof(Stream) });
        static readonly EventInfo rSaveProgress =
            rZipFile.GetEvent(
                "SaveProgress");
        static readonly ConstructorInfo rSaveProgressConstr =
            rSaveProgress.EventHandlerType.GetConstructor(
                 new Type[] { typeof(object), typeof(IntPtr) });
        static readonly Type rEnumType =
            assembly.GetType("Ionic.Zip.ZipProgressEventType");
        static readonly Type rEventArgsType =
            assembly.GetType("Ionic.Zip.SaveProgressEventArgs");
        static readonly object rSaving_AfterWriteEntry =
            Enum.GetValues(rEnumType).Cast<object>()
                .First(v => v.ToString() == "Saving_AfterWriteEntry");
        static readonly PropertyInfo rCancel =
            rEventArgsType.GetProperty("Cancel");
        static readonly PropertyInfo rEventType =
            rEventArgsType.GetProperty("EventType");
        static readonly PropertyInfo rEntriesSaved =
            rEventArgsType.GetProperty("EntriesSaved");
        static readonly PropertyInfo rEntriesTotal =
            rEventArgsType.GetProperty("EntriesTotal");
        static readonly Type rZipEntry =
            assembly.GetType("Ionic.Zip.ZipEntry");
        static readonly MethodInfo rEnumerator =
            rZipFile.GetMethod(
                "GetEnumerator");
        static readonly PropertyInfo rFileName =
            rZipEntry.GetProperty(
                "FileName");
        static readonly MethodInfo rExtract =
            rZipEntry.GetMethod(
                "Extract",
                new Type[] { typeof(Stream) });
    }
}
