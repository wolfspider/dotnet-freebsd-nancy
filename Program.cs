using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Owin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders.Physical;
using System.Linq;
using Nancy.Owin;


namespace RunHostCore
{

    class Program
    {   
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Server!");

            var webhost = new WebHostBuilder()
                .ConfigureAppConfiguration((hostingcontext, config) =>
		{
			config.SetBasePath(Directory.GetCurrentDirectory());
		})
		.UseContentRoot(Directory.GetCurrentDirectory())
		.UseKestrel()
                .UseSockets()
                .UseStartup<Startup>()
                .UseUrls("http://192.168.2.2:5432")
                .Build();

            webhost.Run();

            //Application will stop right here.
        }

        public class PhysicalFileProviderAdapter : IFileProvider
        {
            private readonly PhysicalFileProvider _prodider;
            private static readonly char[] _pathSeparators = new char[2]
            {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
            };
            private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
                .Where(c =>
                {
                    if (c != Path.DirectorySeparatorChar)
                        return c != Path.AltDirectorySeparatorChar;
                    return false;
                }).ToArray();

            public PhysicalFileProviderAdapter(string path)
            {
                _prodider = new PhysicalFileProvider(path);
            }

            internal static bool PathNavigatesAboveRoot(string path)
            {
                StringTokenizer stringTokenizer = new StringTokenizer(path, _pathSeparators);
                int num = 0;
                foreach (StringSegment stringSegment in stringTokenizer)
                {
                    if (!stringSegment.Equals(".") && !stringSegment.Equals(""))
                    {
                        if (stringSegment.Equals(".."))
                        {
                            --num;
                            if (num == -1)
                                return true;
                        }
                        else
                            ++num;
                    }
                }
                return false;
            }

            private string GetFullPath(string path)
            {
                if (PathNavigatesAboveRoot(path))
                {
                    return null;
                }

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(Path.Combine(_prodider.Root, path));
                }
                catch
                {
                    return null;
                }

                if (!IsUnderneathRoot(fullPath))
                {
                    return null;
                }

                return fullPath;
            }

            private bool IsUnderneathRoot(string fullPath)
            {
                return fullPath.StartsWith(_prodider.Root, StringComparison.OrdinalIgnoreCase);
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                if (string.IsNullOrEmpty(subpath) || subpath.IndexOfAny(_invalidFileNameChars) != -1)
                {
                    return new NotFoundFileInfo(subpath);
                }

                // Relative paths starting with leading slashes are okay
                subpath = subpath.TrimStart(_pathSeparators);

                // Absolute paths not permitted.
                if (Path.IsPathRooted(subpath))
                {
                    return new NotFoundFileInfo(subpath);
                }

                var fullPath = GetFullPath(subpath);
                if (fullPath == null)
                {
                    return new NotFoundFileInfo(subpath);
                }

                var fileInfo = new FileInfo(fullPath);
                // MOD: begin
                // this is why we overriding PhysicalFileProviderAdapter -
                // allow serving hidden and files with dots
                /*if (FileSystemInfoHelper.IsHiddenFile(fileInfo))
                {
                    return new NotFoundFileInfo(subpath);
                }*/

                return new PhysicalFileInfo(fileInfo);
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return _prodider.GetDirectoryContents(subpath);
            }

            public IChangeToken ichnge;

            public IChangeToken Watch(string filter)
            {
                return ichnge;
            }

        }

        public class Startup
        {            
        
	    	public void Configure(IApplicationBuilder app)
            	{
                   
               	
			app.UseStaticFiles(new StaticFileOptions{

                    	FileProvider = new PhysicalFileProviderAdapter("/home/lizardking/Downloads/dotnet-sdk/RunHostCore/home")
                    
                });

		app.UseOwin(x => x.UseNancy());

                         
           	}

        
       }

   }
}

