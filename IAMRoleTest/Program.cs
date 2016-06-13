using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace IAMRoleTest
{
    class Program
    {
        const string AccessKey = "<your-aws-access-key>";
        const string SecretKey = "<your-aws-secret>";

        static int Main()
        {
            WriteLine(ConsoleColor.Green,  "========================================");
            WriteLine(ConsoleColor.Green, "| Welcome to the IAM Roles test client |");
            WriteLine(ConsoleColor.Green, "========================================");
            Console.Write("\nHit enter to start: ");
            Console.ReadLine();

            WriteLine(ConsoleColor.Yellow, "========================");
            WriteLine(ConsoleColor.Yellow, "Create the AWS Client(s)");
            WriteLine(ConsoleColor.Yellow, "========================");
            Write(ConsoleColor.Yellow, "Do you want to use the EC2 instance IAM Role (y/n): ");
            var choice = Console.ReadLine();
            var res = TestS3(choice);
            if (res == 0)
            {
                res = TestDynamoDb(choice);
            }

            Console.Write("\nHit enter to exit");
            Console.ReadLine();
            return res;
        }

        private static int TestDynamoDb(string choice)
        {
            var client = GetDynamoDbClient(choice);
            if (client != null)
            {
                LoadItemFromTable(client);
            }
            return client != null ? 0 : 1;
        }

        private static void LoadItemFromTable(IAmazonDynamoDB client)
        {
            WriteLine(ConsoleColor.Green, "\nLoad Item from DynamoDB table");
            WriteLine(ConsoleColor.Green, "=============================================");
            Console.Write("Enter table name (leave empty to skip): ");
            var name = Console.ReadLine();
            Console.Write("Enter id: ");
            var id = Console.ReadLine();
            WriteLine(ConsoleColor.Green, $"Record with id='{id}'");
            WriteLine(ConsoleColor.Green, "=============================================");
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var table = Table.LoadTable(client, name);
                var document = table.GetItem(id);
                WriteLine(ConsoleColor.Black, string.Join(", ",document.Keys));
                WriteLine(ConsoleColor.Blue, string.Join(", ",document.Values.Select(v => v.AsString())));
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.Red, e.Message);
            }
            WriteLine(ConsoleColor.Green, "=============================================\n");
        }

        private static IAmazonDynamoDB GetDynamoDbClient(string choice)
        {
            if (string.IsNullOrWhiteSpace(choice) == false && choice.Trim().ToLower() == "y")
            {
                try
                {
                    // here we use the IAM Role attached to the EC2 instance to get the credentials
                    return new AmazonDynamoDBClient(RegionEndpoint.USWest2);
                }
                catch (Exception ex)
                {
                    WriteLine(ConsoleColor.Red, ex.Message);
                    return null;
                }
            }

            var config = new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 };

            var client = AWSClientFactory.CreateAmazonDynamoDBClient(
                AccessKey,
                SecretKey,
                config
                );
            return client;
        }

        private static int TestS3(string choice)
        {
            var client = GetS3Client(choice);
            if (client != null)
            {
                ListBuckets(client);
                if (CreateBucket(client))
                {
                    ListBuckets(client);
                }
                ListBucketContents(client);
                DownloadToFile(client);
            }
            return client != null ? 0 : 1;
        }

        private static IAmazonS3 GetS3Client(string choice)
        {
            if (string.IsNullOrWhiteSpace(choice) == false && choice.Trim().ToLower() == "y")
            {
                try
                {
                    // here we use the IAM Role attached to the EC2 instance to get the credentials
                    return new AmazonS3Client(RegionEndpoint.USWest2);
                }
                catch (Exception ex)
                {
                    WriteLine(ConsoleColor.Red, ex.Message);
                    return null;
                }
            }

            var config = new AmazonS3Config {RegionEndpoint = RegionEndpoint.USWest2};

            var client = AWSClientFactory.CreateAmazonS3Client(
                AccessKey,
                SecretKey,
                config
                );
            return client;
        }

        private static bool CreateBucket(IAmazonS3 client)
        {
            WriteLine(ConsoleColor.Green, "\nCreate new Bucket");
            WriteLine(ConsoleColor.Green, "=================");
            Console.Write("Enter bucket name (leave empty to skip): ");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name)) return false;
            try
            {
                var request = new PutBucketRequest {BucketName = name};
                client.PutBucket(request);
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.Red, e.Message);
            }
            WriteLine(ConsoleColor.Green, "=================");
            return true;
        }

        private static void ListBuckets(IAmazonS3 client)
        {
            WriteLine(ConsoleColor.Green, "\nList of available Buckets");
            WriteLine(ConsoleColor.Green, "=========================");
            try
            {
                var response = client.ListBuckets();
                foreach (var b in response.Buckets)
                {
                    Console.WriteLine("{0}\t{1}", b.BucketName, b.CreationDate);
                }
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.Red, e.Message);
            }
            WriteLine(ConsoleColor.Green, "=========================");
        }

        private static void ListBucketContents(IAmazonS3 client)
        {
            WriteLine(ConsoleColor.Green, "\nList content of a Bucket");
            WriteLine(ConsoleColor.Green, "=============================================");
            Console.Write("Enter bucket name (leave empty to skip): ");
            var name = Console.ReadLine();
            WriteLine(ConsoleColor.Green, $"Content of Bucket {name}");
            WriteLine(ConsoleColor.Green, "=============================================");
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var request = new ListObjectsRequest {BucketName = name};
                var response = client.ListObjects(request);
                foreach (var o in response.S3Objects)
                {
                    Console.WriteLine("{0}\t{1}\t{2}", o.Key, o.Size, o.LastModified);
                }
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.Red, e.Message);
            }
            WriteLine(ConsoleColor.Green, "=============================================\n");
        }

        private static void DownloadToFile(IAmazonS3 client)
        {
            WriteLine(ConsoleColor.Green, "\nDownload a file from a Bucket");
            WriteLine(ConsoleColor.Green, "=============================================");
            Console.Write("Enter bucket name (leave empty to skip): ");
            var bucketName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(bucketName)) return;
            Console.Write("Enter key: ");
            var key = Console.ReadLine();
            WriteLine(ConsoleColor.Green, $"\nGet file {key} from Bucket {bucketName}");
            WriteLine(ConsoleColor.Green, "=============================================");
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };
                var response = client.GetObject(request);
                key = key.Replace('/', '_');
                var path = Path.Combine(Path.GetTempPath(), key);
                response.WriteResponseStreamToFile($"{path}");
                Console.WriteLine($"File written to: {path}");
            }
            catch (Exception e)
            {
                WriteLine(ConsoleColor.Red, e.Message);
            }
            WriteLine(ConsoleColor.Green, "=============================================\n");
        }

        private static void WriteLine(ConsoleColor color, string text)
        {
            Write(color, text + Environment.NewLine);
        }

        private static void Write(ConsoleColor color, string text)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = prevColor;
        }
    }
}
