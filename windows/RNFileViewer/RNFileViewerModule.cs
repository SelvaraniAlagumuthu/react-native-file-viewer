using ReactNative.Bridge;
using Windows.Storage;
using Windows.System;
using System;
using System.Net.Http;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;
using Windows.UI.Core;
using System.IO;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.IO.Compression;
using Windows.UI.Xaml;
using Windows.Networking.BackgroundTransfer;
using System.ComponentModel;

namespace RNFileViewer
{
    /// <summary>
    /// A module that allows JS to share data.
    /// </summary>
    class RNFileViewerModule : NativeModuleBase
    {
        /// <summary>
        /// The name of the native module.
        /// </summary>
        public override string Name
        {
            get
            {
                return "RNFileViewer";
            }
        }


        [ReactMethod]
        public async void open(string filepath, JObject _, IPromise promise)
        {
            Console.WriteLine("Filepath ==>" + filepath);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
              async () =>
                {

                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(filepath);
                        
                        if (file != null)
                        {
                            var success = await Windows.System.Launcher.LaunchFileAsync(file);

                            if (success)
                            {
                                promise.Resolve(null);
                            }
                            else
                            {
                                promise.Reject(null, "File open failed.");
                            }
                        }
                        else
                        {
                            promise.Reject(null, "File not found.");
                        }
                    }
                    catch (Exception e)//FieldAccessException
                    {
                        promise.Reject(null, filepath, e);
                    }
                }
               );

        }


        [ReactMethod]
        public async void download(string fileName, JObject _, IPromise promise)
        {
            
            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            //  async () =>
            //    {
            try
            {

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder docFolder = KnownFolders.DocumentsLibrary;
                string folderName = "DMSFolder";
                //StorageFolder newFolder = await localFolder.CreateFolderAsync(folderName,CreationCollisionOption.ReplaceExisting);
                //StorageFolder newDocFolder = await docFolder.CreateFolderAsync(folderName,CreationCollisionOption.ReplaceExisting);
                StorageFile file = await localFolder.CreateFileAsync("sample1.zip",CreationCollisionOption.ReplaceExisting);
                StorageFile docfile = await docFolder.CreateFileAsync("sample1.zip",CreationCollisionOption.ReplaceExisting);

                var cli = new HttpClient();
                var uriBing = new Uri(@fileName);
                Byte[] bytes = await cli.GetByteArrayAsync(uriBing);
                IBuffer buffer = bytes.AsBuffer();
                await Windows.Storage.FileIO.WriteBufferAsync(file, buffer);
                await Windows.Storage.FileIO.WriteBufferAsync(docfile, buffer);


                if (file != null)
                {

                    promise.Resolve(null);
                    UnZipFile();
                    //UnZipFile(docfile, KnownFolders.DocumentsLibrary);
                    //    if (success)
                    //    {
                    //        promise.Resolve(null);
                    //    }
                    //    else
                    //    {
                    //        promise.Reject(null, "File open failed.");
                    //    }
                }
                else
                {
                    promise.Reject(null, "File Copied failed.");
                }
            }
            catch (Exception e)//FieldAccessException
            {
                Console.WriteLine("Exception occured====>" + e);
                promise.Reject(null, fileName, e);
            }
            // }
            //);

        }

        [ReactMethod]
        public async void DownloadZipFile(string fileurl, IPromise promise)
        {
            try
            {
                var newFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ZipFolder", CreationCollisionOption.ReplaceExisting);
                var file = await newFolder.CreateFileAsync("sample1.pdf", CreationCollisionOption.ReplaceExisting);

                Uri source = new Uri(fileurl);
                BackgroundDownloader downloader = new BackgroundDownloader();
                DownloadOperation download = downloader.CreateDownload(source, file);
                download.CostPolicy = BackgroundTransferCostPolicy.Always;
                download.Priority = BackgroundTransferPriority.High;

                var success = await download.StartAsync();
                if (file != null)
                {

                    promise.Resolve(null);

                }
                else
                {
                    promise.Reject(null, "File Copied failed.");

                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Excep===>" + e);
                // promise.Reject(null, fileurl, e);
            }
        }


        [ReactMethod]
        public async void UnZipFile()
        {
         StorageFile zipFile =  await KnownFolders.DocumentsLibrary.GetFileAsync("test1.zip");
         StorageFolder   destinationFolder = KnownFolders.DocumentsLibrary;
            if (zipFile == null || destinationFolder == null ||
                !Path.GetExtension(zipFile.Name).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                )
            {
                throw new ArgumentException("Invalid argument...");
            }

            Stream zipMemoryStream = await zipFile.OpenStreamForReadAsync();

            using (ZipArchive zipArchive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    await UnzipZipArchiveEntryAsync(entry, entry.FullName, destinationFolder);
                }
            }
        }

        private static bool IfPathContainDirectory(string entryPath)
        {
            if (string.IsNullOrEmpty(entryPath))
            {
                return false;
            }
            return entryPath.Contains("/");
        }

        private static async Task<bool> IfFolderExistsAsync(StorageFolder storageFolder, string subFolderName)
        {
            try
            {
                await storageFolder.GetFolderAsync(subFolderName);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                throw;
            }
            return true;
        }

        private static async Task UnzipZipArchiveEntryAsync(ZipArchiveEntry entry, string filePath, StorageFolder unzipFolder)
        {
            if (IfPathContainDirectory(filePath))
            {
                string subFolderName = Path.GetDirectoryName(filePath);

                bool isSubFolderExist = await IfFolderExistsAsync(unzipFolder, subFolderName);

                StorageFolder subFolder;

                if (!isSubFolderExist)
                {
                    subFolder =
                        await unzipFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.ReplaceExisting);
                }
                else
                {
                    subFolder =
                        await unzipFolder.GetFolderAsync(subFolderName);
                }

                string newFilePath = Path.GetFileName(filePath);

                if (!string.IsNullOrEmpty(newFilePath))
                {
                    await UnzipZipArchiveEntryAsync(entry, newFilePath, subFolder);
                }
            }
            else
            {
                using (Stream entryStream = entry.Open())
                {
                    byte[] buffer = new byte[entry.Length];
                    entryStream.Read(buffer, 0, buffer.Length);

                    StorageFile uncompressedFile = await unzipFolder.CreateFileAsync
                    (entry.Name, CreationCollisionOption.ReplaceExisting);

                    using (IRandomAccessStream uncompressedFileStream =
                    await uncompressedFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        using (Stream outstream = uncompressedFileStream.AsStreamForWrite())
                        {
                            outstream.Write(buffer, 0, buffer.Length);
                            outstream.Flush();
                        }
                    }
                }
            }
        }



        [ReactMethod]
        public async void openRemote()
        {
            // await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            // {
            //     MainPage mainPage = new MainPage();
            //     mainPage.OpenRemote();
            // });
            // Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            // Console.WriteLine("app installed location===>"+installedLocation);
            //var localFolder = Windows.Storage.KnownFolders.DocumentsLibrary;
            //Console.WriteLine("localFolder location===>" + localFolder);

            // try
            // {
            //     HttpClient client = new HttpClient();
            //     var stream = await client.GetStreamAsync("http://www.adobe.com/content/dam/Adobe/en/accessibility/products/acrobat/pdfs/acrobat-x-accessible-pdf-from-word.pdf");
            //     //var stream = await client.GetStreamAsync(fileIUrl);
            //     var memStream = new MemoryStream();
            //     await stream.CopyToAsync(memStream);
            //     memStream.Position = 0;
            //     PdfDocument doc = await PdfDocument.LoadFromStreamAsync(memStream.AsRandomAccessStream());
            //     Load(doc);
            // }
            // catch(Exception e)
            // {
            //     Console.WriteLine("Exec==>" + e);
            // }



        }

        async void Load(PdfDocument pdfDoc)
        {
            PdfPages.Clear();

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                BitmapImage image = new BitmapImage();

                var page = pdfDoc.GetPage(i);

                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await page.RenderToStreamAsync(stream);
                    await image.SetSourceAsync(stream);
                }

                PdfPages.Add(image);
            }
        }
        public ObservableCollection<BitmapImage> PdfPages
        {
            get;
            set;
        } = new ObservableCollection<BitmapImage>();
        public object Current { get; private set; }
    }
}
