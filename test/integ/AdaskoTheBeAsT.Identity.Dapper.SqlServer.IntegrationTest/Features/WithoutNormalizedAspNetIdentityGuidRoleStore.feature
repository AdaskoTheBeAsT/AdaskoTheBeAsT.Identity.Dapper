Feature: WithoutNormalizedAspNetIdentityGuidRoleStore

A short summary of the feature

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario Outline: Verify lightweight RoleStore method <method> without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	When I verify "<method>" on SQL Server RoleStore without normalized and Guid id
	Then "<method>" on SQL Server RoleStore should work without normalized and Guid id

Examples:
	| method                      |
	| GetRoleIdAsync              |
	| GetRoleNameAsync            |
	| SetRoleNameAsync            |
	| ConvertIdFromString         |
	| ConvertIdToString           |
	| GetNormalizedRoleNameAsync  |
	| SetNormalizedRoleNameAsync  |
	| Dispose                     |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Query Roles without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
		| Auditor       |
	When I execute "Roles" on SQL Server RoleStore
	Then the last roles result for SQL Server RoleStore should match
		| Name          |
		| Administrator |
		| Auditor       |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Create role with CreateAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	When I execute "CreateAsync" on SQL Server RoleStore with parameters
		| Name          | ConcurrencyStamp |
		| Administrator | role-create      |
	Then the last identity result for SQL Server RoleStore should be successful
	And the last role result for SQL Server RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Update role with UpdateAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	When I execute "UpdateAsync" on SQL Server RoleStore with parameters
		| RoleName      | NewName         | NewConcurrencyStamp |
		| Administrator | Administrators  | role-updated        |
	Then the last identity result for SQL Server RoleStore should be successful
	And the last role result for SQL Server RoleStore should match
		| Name           |
		| Administrators |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Delete role with DeleteAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	When I execute "DeleteAsync" on SQL Server RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last identity result for SQL Server RoleStore should be successful
	And the last role result for SQL Server RoleStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByIdAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByIdAsync" on SQL Server RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for SQL Server RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByNameAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByNameAsync" on SQL Server RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for SQL Server RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get role claims with GetClaimsAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	And I added role claims for SQL Server RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "GetClaimsAsync" on SQL Server RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last claims result for SQL Server RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |
		| permission | audit-users  |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add role claim with AddClaimAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	When I execute "AddClaimAsync" on SQL Server RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for SQL Server RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove role claim with RemoveClaimAsync without normalized and Guid id
	Given I have configured SQL Server RoleStore without normalized and Guid id
	And I created roles for SQL Server RoleStore
		| Name          |
		| Administrator |
	And I added role claims for SQL Server RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "RemoveClaimAsync" on SQL Server RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for SQL Server RoleStore should match
		| ClaimType  | ClaimValue  |
		| permission | audit-users |
