# Dynamic Object Management System

-   This project is a \*\*Dynamic Object Management System\*\* built
    using \*\*ASP.NET Core\*\*. It allows users to dynamically create
    objects with custom schemas, manage relationships between objects,
    and perform CRUD operations (Create, Read, Update, Delete) on them.
    The system supports dynamic object filtering based on custom fields
    and values.

## \## Technologies

-   ASP.NET Core 8.0

-   Entity Framework Core Code First

-   PostgreSQL

-   n-Layer Architecture

-   Newtonsoft.Json (JSON Serialization)

-   Exception Handling Middleware

-   Generic Repository Design Pattern

-   Async Programming

-   Dependency Injection

-   Transaction with Autofac (Interceptor)

## **\## Process of Project**

### **1.Adding Schemas For Objects That Wanted To Add**

-   Users must first create the type and fields of the object they want
    to add. The object type is sent as string and the fields are sent as
    JSON. The fields required for validation must also be sent in
    JSON.("required":)

![](Images/ObjectSchemaAddRequest.png) Image 1 : CreateObjectSchema request

-   The schema sent with the request is stored in the ObjectSchema
    table.

![](./99536f76054764fb67c00ebcdd94580626d1f172.png){width="6.260415573053368in"
height="0.7291666666666666in"} Image 2 : Example ObjectSchema Table

### 

### **2.Adding Datas According to ObjectSchema**

-   The data to be added is sent as a string in ObjectType and as JSON
    in data.

![](./b29d523e1683bf942453bfa433b951f71ba2acfd.png){width="6.228759842519685in"
height="3.6273982939632545in"} Image 4 : Example AddObjectData request

-   The master-object and sub-objects in the incoming request are
    validated. If no problems occur, they are added to the database.

> ![](./6dec035c801bab2e67cc93fb59780101bbf094ee.png){width="6.260415573053368in"
> height="0.8333333333333334in"} Image 5 : Display of Database after
> adding data

-   If there is a problem with the validation of the master-object and
    sub-object, the Global Exception Handler throws an error.

> ![](./b94628e79226101de5fcd0eb4b325b29688a5d8d.png){width="6.260415573053368in"
> height="1.1666666666666667in"} Image 6 : Exception result of
> validation errors

## 

## **3.Get Filtered Datas**

-   For filtering, conditions are sent as ObjectType and JSON as
    strings. Filters are sent as "lt": (less than) for greater than,
    "gt": (greater than) for less than, and "startsWith": x starts with
    a letter.

![](./80d6f305448363d4cf08bb17146aeb00439b4c6e.png){width="6.260415573053368in"
height="1.6458333333333333in"} Image 7 : Example of filter request

![](./69cb6b44f17cffcd15528eb03d10642a3dd28c45.png){width="6.260415573053368in"
height="2.0833333333333335in"} Image 8 : Result of filter request

## **4.Updating Datas**

-   The data to be updated is sent as an object, just like in the
    addition process, and the data to be updated is updated by
    performing master-object and sub-object validations.

![](./f584be7823208d8f9ebc0d21894fa242446d7f93.png){width="6.260415573053368in"
height="3.28125in"} Image 9 : Example Update Request

## **5. Deleting Data**

-   The data to be deleted is deleted by giving its id via the route. At
    the same time, the sub-object data within the data is also deleted.

![](./e083971421cac4757f92011920776b046214820d.png){width="6.260415573053368in"
height="0.7083333333333334in"} Image 10 : Example Delete Request

![](./e4f39178fb718b8095c9f0a3fc186d6c72381305.png){width="6.260415573053368in"
height="1.2291666666666667in"} Image 11 : Database after Deleting
Master-Object and Its Sub-Objects

![](./81d94b8731e7fed5578d500da94f5572b3580e9e.png){width="6.260415573053368in"
height="2.2916666666666665in"}
