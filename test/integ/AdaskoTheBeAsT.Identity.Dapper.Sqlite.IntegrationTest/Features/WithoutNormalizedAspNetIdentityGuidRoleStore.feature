Feature: WithoutNormalizedAspNetIdentityGuidRoleStore

A short summary of the feature

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario Outline: Verify lightweight RoleStore method <method> without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	When I verify "<method>" on SQLite RoleStore without normalized and Guid id
	Then "<method>" on SQLite RoleStore should work without normalized and Guid id

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
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
		| Auditor       |
	When I execute "Roles" on SQLite RoleStore
	Then the last roles result for SQLite RoleStore should match
		| Name          |
		| Administrator |
		| Auditor       |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Create role with CreateAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	When I execute "CreateAsync" on SQLite RoleStore with parameters
		| Name          | ConcurrencyStamp |
		| Administrator | role-create      |
	Then the last identity result for SQLite RoleStore should be successful
	And the last role result for SQLite RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Update role with UpdateAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	When I execute "UpdateAsync" on SQLite RoleStore with parameters
		| RoleName      | NewName         | NewConcurrencyStamp |
		| Administrator | Administrators  | role-updated        |
	Then the last identity result for SQLite RoleStore should be successful
	And the last role result for SQLite RoleStore should match
		| Name           |
		| Administrators |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Delete role with DeleteAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	When I execute "DeleteAsync" on SQLite RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last identity result for SQLite RoleStore should be successful
	And the last role result for SQLite RoleStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByIdAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByIdAsync" on SQLite RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for SQLite RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByNameAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByNameAsync" on SQLite RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for SQLite RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get role claims with GetClaimsAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	And I added role claims for SQLite RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "GetClaimsAsync" on SQLite RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last claims result for SQLite RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |
		| permission | audit-users  |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add role claim with AddClaimAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	When I execute "AddClaimAsync" on SQLite RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for SQLite RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove role claim with RemoveClaimAsync without normalized and Guid id
	Given I have configured SQLite RoleStore without normalized and Guid id
	And I created roles for SQLite RoleStore
		| Name          |
		| Administrator |
	And I added role claims for SQLite RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "RemoveClaimAsync" on SQLite RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for SQLite RoleStore should match
		| ClaimType  | ClaimValue  |
		| permission | audit-users |
