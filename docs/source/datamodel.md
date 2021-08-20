# Data Model And Concepts

DRAFT

One of the central features of omnikeeper is its simple and flexible data model: CIs, attributes and relations hold all the data. There are no restrictions on what data a CI can hold or represent. It could model a piece of IT hardware, a virtualized service, a network interface, a user-group or any other entity. It's not required to define complex data types or schemas beforehand.  
This approach allows omnikeeper to keep up with evolving requirements to its data model. Expanding or otherwise modifying existing CIs does not require costly data schema re-designs and implementation changes. New "types" of CIs can be added much easier. Users and developers simply have more room to move.

There is a danger to this approach of modelling data. Without good tools and mechanisms, the data can become hard to understand and work with. When working with any non-trivial amount of data, you'll always want to form groups of entities that are - for the current purpose - the same and then treat them all in the same way. But because each CI has the potential to change in different ways, two CIs that arguably model entities of the same "type" can look totally different when looking at their data. 

Traits solve the two problems mentioned above:
- allow selection of a specific subset of CIs within the full data set
- guarantee that each of the CIs in this subset fulfills certain requirements, allowing requestors to work with this data easily

Traits are a powerful omnikeeper feature and offer a lot of control, yet stay flexible. [[Click here To learn more about traits|traits]].

## Data ingest and validation
When importing data into omnikeeper, the process of data validation and putting entities into types/groups/categories is - by default - shifted back into other regions of omnikeeper. This is done for a couple of reasons:
- we've seen in practice that there are a lot of situations where people/a team/a service is in control of data that would be very valuable to other people/teams/services. Yet they are reluctant to share this data and open a data channel because of hard requirements on the receiving side. Making it as easy as reasonable for these parties to share their data encourages collaboration and exchange of vital informations.  
It is important that parties that enter data into omnikeeper keep a certain amount of authority over that data. Allowing them to make changes in their data without too much friction is vital. After all, they offer the data that other parties need.
- what constitutes a valid entity and what is not is not a fixed anchor point that will stay for all time. Just as all other things are subject to changes, validating data is too. Allowing data to enter first without going through a rigorous validation step can help in this change process.
- talking about "types" of entities/CIs: forcing data producers to conform their data into types of a central type system is very rigid and leads to a lot of friction. The shift back allows data to be much more composable. Consider the example where multiple parties each hold a piece of the full data. Consumers on the other side want to select data pieces as they see fit for their respective usecases. By allowing each data producer to insert their own slice of data into omnikeeper, even if that information is not that useful by itself, gives consumers more options and data to work with.
- being able to shift back validation does not mean that it HAS to be. When the usecase calls for it, omnikeeper offers data interfaces that perform validation and reject data that does not conform.

Of course, shifting back responsibilities from the incoming side has its challenges. The cases where no structure to the data is required are seldom. Somebody at some point must be responsible for applying structure, otherwise it is at worst a heap of garbage data. omnikeeper offers concepts and features to get ahold of the data again. The two main ones are [[traits|traits]] and [[layers|layers]].
