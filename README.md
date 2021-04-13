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

### Database SQL
```
CREATE TABLE 'sample_database'.'sample_table' (
  'id' BIGINT(20) NOT NULL AUTO_INCREMENT,
  'active' TINYINT(1) NULL DEFAULT 1,
  'InternalId' VARCHAR(45) UNIQUE NULL,
  'create_date' TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
  'last_updated' TIMESTAMP NULL,
  PRIMARY KEY ('id')),
  UNIQUE INDEX `InternalId_UNIQUE` (`InternalId` ASC));
  
  
DROP TRIGGER IF EXISTS 'sample_database'.'sample_table_BEFORE_INSERT';
DELIMITER $$
USE 'sample_database'$$
CREATE DEFINER = CURRENT_USER TRIGGER 'sample_database'.'sample_table_BEFORE_INSERT' BEFORE INSERT ON 'sample_table' FOR EACH ROW
BEGIN
set new.InternalId = IFNULL(new.InternalId, uuid());
set NEW.last_updated = CURRENT_TIMESTAMP;
END$$

DELIMITER ;

DROP TRIGGER IF EXISTS 'sample_database'.'sample_table_BEFORE_UPDATE';
DELIMITER $$
USE 'sample_database'$$
CREATE DEFINER = CURRENT_USER TRIGGER 'sample_database'.'sample_table_BEFORE_UPDATE' BEFORE UPDATE ON 'sample_table' FOR EACH ROW
BEGIN
set NEW.last_updated = CURRENT_TIMESTAMP;
END$$

DELIMITER ;
```

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

DTO Creation
------------
Sometimes it's necessary to send a data transfer of an object to another context, such as when responding to a REST request, where you may want to minimize the information or size of package being sent. This is where DTOs (data transfer objects) come into play, and are very easy to configure and execute within DALHelperNet; you don't even need to create new objects.

DTO object inclusion is executed on an opt-in basis using your already-created DALHelper objects. In order to mark an object's property for inclusion, simply put the "[DALTransferable]" attribute on properties you want to be in the DTO. When you're ready to create the DTO, simply call "GenerateDTO()" on any object that inherits from DALBaseModel.

POCO Decorations
----------------
In order to enable a C# object for DALHelper use, there are some attributes that need to be added in order to "connect" them to the database. These attributes have some options each on them, all described below.

`[DALTable("database_table_name")]`
DALTable is used at the top of a class definitions for DALHelper's automatic output build function. The option this attribute takes is the literal string name of the database table this object writes out to by default.

`[DALResolvable("column_name")]`
DALResolvable is added to each property you wish to connect to a column in the database. The __optional__ property can be used to force a different column name other than the automatic one for reading and writing.

`[DALTransferable]`
DALTransferable should be added to each property that you wish to include in the DTO.

Constructors
