using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace AzureStorageSaveAndSecure
{
	class Program
	{
		private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=floeudevfiles;AccountKey=7IGLzbSyuUMHRQxQx+tgo+noscufYcGj3mzQVx8h3MWvc7jqYpaNkwp2uPBXmNq/KfqIUubO1lbFrzJlU1p9SA==;BlobEndpoint=https://local-eu-files.wwolf.io;";
		private const string BlobContainerName = "backoffice";
		private const string BlobName = "testfile.txtx";
		
		static async Task Main(string[] args)
		{

			var serviceClient = new BlobServiceClient(ConnectionString);

			var containerClient = serviceClient.GetBlobContainerClient(BlobContainerName);

			await containerClient.CreateIfNotExistsAsync();

			var client = containerClient.GetBlobClient(BlobName);

			await client.UploadAsync(GenerateStreamFromString("this is a test file"), overwrite:true);


			GetAdHocBlobSasToken(BlobContainerName, BlobName);
		}

		public static string GetAdHocBlobSasToken(string containerName, string blobName)
		{
			var sasBuilder = new BlobSasBuilder()
			{
				BlobContainerName = containerName,
				BlobName = blobName,
				Resource = "b",//Value b is for generating token for a Blob and c is for container
				StartsOn = DateTime.UtcNow.AddMinutes(-2),
				ExpiresOn = DateTime.UtcNow.AddMinutes(10),
			};

			sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write); //multiple permissions can be added by using | symbol

			var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(GetKeyValueFromConnectionString("AccountName"), GetKeyValueFromConnectionString("AccountKey")));

			var secureUrl = $"{new BlobClient(ConnectionString, containerName, blobName).Uri}?{sasToken}";
			Console.WriteLine(secureUrl);

			return secureUrl;
		}

		private static Stream GenerateStreamFromString(string s)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}

		private static string GetKeyValueFromConnectionString(string key)
		{
			IDictionary<string, string> settings = new Dictionary<string, string>();
			var splitted = ConnectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var nameValue in splitted)
			{
				var splittedNameValue = nameValue.Split(new[] { '=' }, 2);
				settings.Add(splittedNameValue[0], splittedNameValue[1]);
			}

			return settings[key];
		}
	}
}
