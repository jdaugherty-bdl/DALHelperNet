# DALHelperNet
DALHelperNet is a lightweight database access library. It's meant to make accessing, manipulating, and writing data via POCO objects easier.

Database structure
------------------
There is a boilerplate database table template that is required for this to work. Underscore placement in column names is important as DALHelper uses those to convert to CapitalCase. Only the specific word "InternalId" should be capitalized as such, and without underscores.

### Columns
Column Name | Type | AI/PK | UQ
------------|------|-----|----
id | bigint | Y |
... \<your custom fields\>
active | tinyint | |
InternalId | string(45) | | Y
create_date | timestamp | |
last_updated | timestamp | |

### Triggers
* _On insert_ - if InternalId is NULL, assign a new UUID4 (GUID) to `InternalId`, and update `last_updated`
* _On update_ - update `last_updated`



Underscore names
----------------
There is a standard conversion algorithm to convert POCO property names to underscore-cased database column names: all properties that have capital letters within the body of the property name will have an underscore placed before that capital letter. This applies to everything except the specific string "InternalId".

*Examples*
POCO Property|Database Underscore Name
-------------|------------------------
Active | active
CreateDate | create_date
MyObjectColumnName | my_object_column_name
InternalId | InternalId
MyObjectsInternalId | my_objects_InternalId
