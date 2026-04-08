Feature: WithoutNormalizedAspNetIdentityGuidRoleStore

A short summary of the feature

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario Outline: Verify lightweight RoleStore method <method> without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	When I verify "<method>" on MySQL RoleStore without normalized and Guid id
	Then "<method>" on MySQL RoleStore should work without normalized and Guid id

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
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
		| Auditor       |
	When I execute "Roles" on MySQL RoleStore
	Then the last roles result for MySQL RoleStore should match
		| Name          |
		| Administrator |
		| Auditor       |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Create role with CreateAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	When I execute "CreateAsync" on MySQL RoleStore with parameters
		| Name          | ConcurrencyStamp |
		| Administrator | role-create      |
	Then the last identity result for MySQL RoleStore should be successful
	And the last role result for MySQL RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Update role with UpdateAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	When I execute "UpdateAsync" on MySQL RoleStore with parameters
		| RoleName      | NewName         | NewConcurrencyStamp |
		| Administrator | Administrators  | role-updated        |
	Then the last identity result for MySQL RoleStore should be successful
	And the last role result for MySQL RoleStore should match
		| Name           |
		| Administrators |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Delete role with DeleteAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	When I execute "DeleteAsync" on MySQL RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last identity result for MySQL RoleStore should be successful
	And the last role result for MySQL RoleStore should be null

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByIdAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByIdAsync" on MySQL RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for MySQL RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Find role with FindByNameAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	When I execute "FindByNameAsync" on MySQL RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last role result for MySQL RoleStore should match
		| Name          |
		| Administrator |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Get role claims with GetClaimsAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	And I added role claims for MySQL RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "GetClaimsAsync" on MySQL RoleStore with parameters
		| RoleName      |
		| Administrator |
	Then the last claims result for MySQL RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |
		| permission | audit-users  |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Add role claim with AddClaimAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	When I execute "AddClaimAsync" on MySQL RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for MySQL RoleStore should match
		| ClaimType  | ClaimValue   |
		| permission | manage-users |

@xunit:collection[DatabaseWithGuidIdCollection]
Scenario: Remove role claim with RemoveClaimAsync without normalized and Guid id
	Given I have configured MySQL RoleStore without normalized and Guid id
	And I created roles for MySQL RoleStore
		| Name          |
		| Administrator |
	And I added role claims for MySQL RoleStore
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
		| Administrator | permission | audit-users  |
	When I execute "RemoveClaimAsync" on MySQL RoleStore with parameters
		| RoleName      | ClaimType  | ClaimValue   |
		| Administrator | permission | manage-users |
	Then the last claims result for MySQL RoleStore should match
		| ClaimType  | ClaimValue  |
		| permission | audit-users |
