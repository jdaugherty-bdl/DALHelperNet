# DALHelperNet
DALHelperNet is a lightweight database access library. It's meant to make accessing, manipulating, and writing data via POCO objects easier.

There is a boilerplate database table template that is required for this to work. Underscore placement in column names is important as DALHelper uses those to convert to CapitalCase.

Columns
-------
id - autonumber
... - <your custom fields>
active - tinyint
InternalId - string(45) - unique
create_date - timestamp
last_updated - timestamp

Triggers
--------
On insert - if InternalId is NULL, assign a new UUID4 (GUID) to `InternalId`, and update `last_updated`
On update - update `last_updated`
