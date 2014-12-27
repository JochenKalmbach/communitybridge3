# Forums users
The Forums APIs use an API key which is passed through the x-ms-apikey header. To obtain an API key please contact msdnsupp@microsoft.com.

## Get user details

```httprequest
GET https://forumsapi.contentservices.msdn.microsoft.com/users/{id}
```

| Parameter (URL)   | Type     | Default | Notes
|:------------|:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| id          | string   |         | User id

#### Header
```header
x-ms-apikey: {API-KEY}
```

### By user id

#### Sample request
```httprequest
GET https://forumsapi.contentservices.msdn.microsoft.com/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [
        {
            "id":"5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "displayName":"Nigel Beasley",
            "roles" : [ "communityContributor" ],
            "url" :"https://forumsapi.contentservices.msdn.microsoft.com/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "webUrl": "http://social.msdn.microsoft.com/Profile/Nigel%20Beasley",
            "points" : 7,
            "messagesCount" : 14,
            "answersCount" : 5
        }
    ]
}
```

## search users

```httprequest
GET https://forumsapi.contentservices.msdn.microsoft.com/users[?userId={string}[,{string}]&userName={string}[,{string}]&role={string}[,{string}]&sort={string}&order={string}&page={integer}&pageSize={integer}&api-version={string}]
```

| Parameter (Query)      | Type     | Default | Notes
|:-----------    |:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| userId           | string   |         | Id of the user.  A comma-separated list of user ids may be supplied.
| userName           | string   |         | Display name of the user.  A comma-separated list of user names may be supplied.
| role           | string   |         | The role of the user.  Allowed roles are: "microsoftEmployee", "microsoftValuableProfessional", "partner", "microsoftSupportStaff", "microsoftContingentStaff", and "communityContributor". A comma-separated list of roles may be supplied. If multiple roles are provided, a user will be included in the result set if any role is matched.
| sort           | string   | points  | The sort order of returned users.  Valid values are "displayName", "points", "answersCount", "messagesCount".
| order          | string   | desc    | The sort order of returned threads.  Valid values are "asc", "desc".
| page           | integer  | 1       | When the number of results exceeds the maximum page size, then the page parameter indicates which page of results to return.  This value is unit-based, e.g. the first page of results is indicated by ?page=1
| pageSize       | integer  | 50      | The number of results to return per page.  This value cannot exceed 50.
| api-version    | float    | 1.0     | The version of the API to use. Refer to [Versioning](#versioning) for more details.


### By multiple IDs

#### Sample request
```httprequest
GET https://forumsapi.contentservices.msdn.microsoft.com/users?userId=5812d39c-2064-4fee-ba3e-eb9716fb7a37,26b8b233-d0a6-41cc-859c-eb96b2225607
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [ 
        {
            "id":"5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "displayName": "Nigel Beasley",
            "roles" : [ "microsoftValuedProfessional" ],
            "url" :"https://forumsapi.contentservices.msdn.microsoft.com/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "webUrl": "http://social.msdn.microsoft.com/Profile/Nigel%20Beasley",
            "points" : 7,
            "messagesCount" : 14,
            "answersCount" : 5
        },
        {
            "id":"26b8b233-d0a6-41cc-859c-eb96b2225607",
            "displayName": "larrysir",
            "roles" : [ "microsoftContingentStaff" ],
            "url" :"https://forumsapi.contentservices.msdn.microsoft.com/users/26b8b233-d0a6-41cc-859c-eb96b2225607",
            "webUrl": "http://social.msdn.microsoft.com/Profile/larrysir",
            "points" : 0,
            "messagesCount" : 2,
            "answersCount" : 0
        }
    ]
}
```


## Errors

REST API operations for forums services return standard HTTP status codes, as defined in the HTTP/1.1 Status Code Definitions.  API operations my also return additional error information in the response body.

The body of the error response follows the JSON format shown here.

#### Sample response

```json
{
    "errors" : [
            "Unrecognized parameter: 'from'. ",
            "Unrecognized parameter: '042870'."
    ]
}
```
<a name="versioning"></a>
## Versioning 

The Forums API is versioned to ensure client applications and services work as APIs are added and changed. The current version of the API set is 1.0-preview. Specifying API Version is optional for GET requests and required for POST/PUT/UPDATE/DELETE. If not specified, it defaults to the latest. API version can be specified either in the header of the HTTP request or as a URL query parameter.

HTTP request header:
```httprequest
Accept: application/json;api-version=1.0-preview
```

Query parameter:
```httprequest
GET https://forumsapi.contentservices.msdn.microsoft.com/7a9b8c49-06ac-44fe-b0fd-81485bd31386?api-version=1.0-preview
```