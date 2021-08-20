# Comparisons

## vs. raw relational/SQL databases
When designing applications that handle any kind of data, developers often opt to use relational/SQL databases. Such database systems force a lot of constraints on the data model's design right from the start. when choosing a relational database model, great care has to be put into its data schema. Changes later in development are often costly. And yet, applications that provide a benefit rarely stay static. Stakeholders and other users demand new features and improvements to existing features, usecases and requirements evolve. The application's data model has to keep up... or users will get creative. 
During the lifecycle of a typical data centric application, a certain amount of degredation in the data's quality is unavoidable. However, this degradation in quality is exacerbated by rigid data models that cannot keep up with ever-changing requirements and usecases. People start to misuse data fields for different purposes than initially intented, work around missing features with hacks and tricks and do all kinds of magic to get the application to do what they want. Data quality suffers, and the knowledge on how to interpret the inferior data becomes more and more sparse as well. And once people form the opinion that the data that an application provides is bad, they are going to start looking for alternatives and more workarounds emerge.

## vs. raw NoSQL databases
All these problems can be reduced greatly by employing a more flexible data model that can bend and grow along with its usecases. NoSQL databases offer an alternative approach to data modelling, removing a lot of the rigidity and up-front design requirements.  
However, for certain usecases, NoSQL databases take it too far and do not provide enough tools to work with its unstructured data. 
Furthermore, NoSQL databases often lack the features that omnikeeper provides, such as the temporal approach of keeping a log of all changes, layers or authorization concepts.

## vs. CMDBs
TODO

## vs. Enterprise Service Buses (ESB)
ESBs:
- Nachrichten basierend (Messaging-Integration)​
- Übertragung und Umformung von Nachrichten​
- (externe) Vorgänge auslösen​
- Keine Datenhaltung​
- Konzeptionell “kleine Häppchen”​

omnikeeper:
- Daten-Integration​
- Routing, Transformation und Kombination von Daten​
- Use Case angepasste Inhalte für Datenkonsumenten bereitstellen​
- Datenhaltung für​ Metainformation, Caching, Anreicherung​
- Konzeptionell “große Brocken” (batch processing)​

- Aufgrund der Arbeitsweise bereits vorhanden:​
    - Berechtigungen​
    - Auditsicher: Versionierung, Zugriffe​
    - Diffing​
    - Traits​
    - Compute Layer​
