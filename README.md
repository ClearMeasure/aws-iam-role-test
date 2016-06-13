#Introduction
Goal is to use IAM roles to access AWS resources from an application running on EC2. The IAM role supplies temporary permissions that applications can use when they make calls to other AWS resources.

# Configuration
* When launching an EC2 instance, specify an IAM role to associate with the instance.
* Applications that run on the instance can then use the role-supplied temporary credentials to sign API requests.
* Need to create an *instance profile* that is attached to the EC2 instance
* Only ONE role can be assigned to an EC2 instance at a time
* All applications on the instance share the same role and permissions
* can use same role for multiple instances
* Role needs to be assigned to instance at launch time!
* developers doesn't need permission to access resources and doesn't have to deal with credentials at all

* security credentials are temporary and automatically rotated by AWS
* retrieve temporary credentials from EC2 instance metadata like this `iam/security-credentials/role-name`, e.g.

  `$ curl http://169.254.169.254/latest/meta-data/iam/security-credentials/s3access`

  response is like this

  ```
  {
    "Code" : "Success",
    "LastUpdated" : "2012-04-26T16:39:16Z",
    "Type" : "AWS-HMAC",
    "AccessKeyId" : "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey" : "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "Token" : "token",
    "Expiration" : "2012-04-27T22:39:16Z"
  }
  ```

## S3 Policy

Define a **custom** S3 Bucket Policy (which is a copy of a stock policy that you change) that allows access to particular buckets, e.g.

  ```
  {
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "s3:Get*",
                "s3:List*"
            ],
            "Resource": [
                "arn:aws:s3:::mar-uat",
                "arn:aws:s3:::mar-uat-*"
            ]
        }
    ]
  }
  ```

Attach this policy to the IAM role.

## DynamoDB Policy

Define a **custom** DynamoDB Bucket Policy (which is a copy of a stock policy that you change) that allows access to particular tables, e.g.

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Action": [
                "dynamodb:GetItem",
                "dynamodb:BatchGetItem",
                "dynamodb:Query"
            ],
            "Effect": "Allow",
            "Resource": "arn:aws:dynamodb:::dynamodb:table/UAT-*"
        }
    ]
}
```

Attach this policy to the IAM role.

#Code

To create an .NET application that can access AWS resources add the AWS SDK nuget package to the respective project.

To create a client that can access S3 and uses the credentials from the IAM role attached to the EC2 instance we simply need to use a statement like this:

```
var client = new AmazonS3Client(RegionEndpoint.USWest2);
```

**Note:** select the appropriate **region**.

This is much more convenient than using explicit API keys and secrets.
Sample code to list all objects in a given S3 bucket

```
var request = new ListObjectsRequest {BucketName = name};
var response = client.ListObjects(request);
foreach (var o in response.S3Objects)
{
    Console.WriteLine("{0}\t{1}\t{2}", o.Key, o.Size, o.LastModified);
}
```

or to download an object and store it as a file

```
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
``` 

here the `key` is the full path of the file in the respective bucket. E.g. if we have a file called `sample.txt` in a folder `one` then the value of the key would be `one/sample.txt`

