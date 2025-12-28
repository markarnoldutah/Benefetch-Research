Key Patterns:
1.	required init - For properties that must be provided at creation and never change (IDs, codes, snapshots)
2.	required set - For properties that must be provided but can be updated later (names, dates that can be rescheduled)
3.	{ get; set; } = <default> - For properties with sensible defaults (Status = "scheduled", IsEnabled = true)
4.	...? { get; set; } - For optional, mutable properties
5.	...? { get; init; } - For optional, immutable properties
6.	=> Expression - For computed properties (PayerId => Id, PatientId => Id)


