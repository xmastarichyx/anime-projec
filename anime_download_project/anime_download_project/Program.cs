using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace anime_download_project
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string mainUrl = "https://sereinscan.com/";
            string baseSaveDirectory = @"C:\Users\mastarichy\Desktop\images\"; // Ana kaydetme dizini
            Directory.CreateDirectory(baseSaveDirectory);
            Console.WriteLine("Web Sitesini Girin: ");
            mainUrl = Console.ReadLine();

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Ana sayfanın HTML içeriğini indir
                    string mainHtml = await client.GetStringAsync(mainUrl);

                    // Ana sayfanın linklerini parse et
                    List<string> links = GetLinksFromHtml(mainHtml, mainUrl);

                    // Her bir linkteki resimleri indir
                    foreach (string link in links)
                    {
                        // Linke özgü bir klasör oluştur
                        string linkFolderName = GetSafeFolderName(link);
                        string linkSaveDirectory = Path.Combine(baseSaveDirectory, linkFolderName);
                        Directory.CreateDirectory(linkSaveDirectory);

                        // Linkin resimlerini indir ve klasöre kaydet
                        await DownloadImagesFromLink(client, link, linkSaveDirectory);
                    }

                    // İndirme işlemi tamamlandı mesajı
                    Console.WriteLine("İndirme işlemi tamamlandı.");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {ex.Message}");
                }
            }
        }

        // HTML içeriğinden tüm linkleri çıkaran metot
        static List<string> GetLinksFromHtml(string html, string baseUrl)
        {
            var links = new List<string>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // <a> etiketlerindeki href'leri al
            var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkNodes != null)
            {
                foreach (var node in linkNodes)
                {
                    string href = node.GetAttributeValue("href", "");

                    // Tam URL değilse, tam hale getirin
                    if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
                    {
                        Uri baseUri = new Uri(baseUrl);
                        href = new Uri(baseUri, href).ToString();
                    }

                    links.Add(href);
                }
            }

            return links;
        }

        // Güvenli klasör adı oluşturma metodu
        static string GetSafeFolderName(string url)
        {
            // URL'yi geçerli bir klasör adına çevirin
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                url = url.Replace(c, '_'); // Geçersiz karakterleri alt çizgi ile değiştir
            }
            // İstemediğiniz eklemeleri (örneğin, protokol tanımlamaları gibi) kaldırabilirsiniz
            return url.Replace("https://", "").Replace("http://", "").Replace("/", "_");
        }

        // Verilen linkteki tüm resimleri indiren metot
        static async Task DownloadImagesFromLink(HttpClient client, string url, string saveDirectory)
        {
            try
            {
                // Linkin HTML içeriğini indir
                string html = await client.GetStringAsync(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Tüm <img> etiketlerini bul ve src değerlerini al
                var imageNodes = doc.DocumentNode.SelectNodes("//img[@src]");
                if (imageNodes != null)
                {
                    foreach (var node in imageNodes)
                    {
                        string imageUrl = node.GetAttributeValue("src", "");

                        // Mutlak yol değilse, URL'i tam hale getirin
                        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                        {
                            Uri baseUri = new Uri(url);
                            imageUrl = new Uri(baseUri, imageUrl).ToString();
                        }

                        // Resim adını URL'den al ve dosya yolunu oluştur
                        string fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                        string filePath = Path.Combine(saveDirectory, fileName);

                        // Resmi indir ve kaydet (Asenkron metot kullanıldı.)
                        byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
                        await SaveImageAsync(filePath, imageBytes);

                        Console.WriteLine($"Resim indirildi: {filePath}");
                    }
                }
                else
                {
                    Console.WriteLine($"Hiç resim bulunamadı: {url}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu ({url}): {ex.Message}");
            }
        }

        // Asenkron olarak resmi kaydetme metodu
        static async Task SaveImageAsync(string filePath, byte[] imageBytes)
        {
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
            }
        }
    }
}
