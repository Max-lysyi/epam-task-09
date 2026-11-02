@level1
Feature: Level 1 - View Participants (UI & API)
  As the Owner of a group reservation
  I want to view all participants, their chosen dishes, quantities, and individual subtotals
  So that I can monitor the pre-order status and total cost before submitting to the restaurant

  @api @ui @smoke @happy_path
  Scenario: Owner views participants, dishes, individual subtotals, and dynamic grand total
    Given an active group reservation exists with an Owner and 2 guests:
      | Name     | Type  | IsSubmitted | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | Spring Salad | 12.00 | 1        |
      | Olena    | Guest | true        | Tom Yum      | 22.00 | 2        |
      | Andrii   | Guest | true        | Caesar Salad | 18.00 | 1        |
    When the Owner opens the group pre-order review page
    Then the Owner sees all 3 participants in the list
    And the participant "Yevhenii" has subtotal 12.00
    And the participant "Olena" has subtotal 44.00
    And the participant "Andrii" has subtotal 18.00
    And the Grand Total equals 74.00
    When the guest "Andrii" updates their cart to 2 "Caesar Salad" with price 18.00 via API
    And the Owner refreshes the page
    Then the participant "Andrii" has subtotal 36.00
    And the Grand Total equals 92.00

  @ui @edge_case
  Scenario: Unsubmitted participants are visually marked and notice is displayed
    Given an active group reservation exists with an Owner and 2 guests:
      | Name     | Type  | IsSubmitted | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | Spring Salad | 12.00 | 1        |
      | Mariia   | Guest | false       | Tom Yum      | 22.00 | 1        |
    When the Owner opens the group pre-order review page
    Then the participant "Mariia" is marked with status "Still choosing"
    And a notice is displayed stating participants have not submitted their cart yet

  @api @edge_case
  Scenario: An empty group displays empty participants list and total 0
    Given an active group reservation exists with no participants
    When the Owner requests the participants list via API
    Then the API response status code is 200
    And the API response contains 0 participants
    And the API response total is 0.00

  @api @negative
  Scenario: Requesting participants for a non-existent group returns 404
    When the Owner requests the participants list for a non-existent group "non-existent-group-999" via API
    Then the API response status code is 404

  @api @authorization
  Scenario Outline: Only the Owner can view the group participants list
    Given a valid group reservation exists
    When a user with role "<Role>" requests the participants list via API
    Then the API response status code is <ExpectedStatusCode>

    Examples:
      | Role         | ExpectedStatusCode |
      | Owner        | 200                |
      | Guest        | 403                |
      | Waiter       | 403                |
      | OtherClient  | 403                |
      | Unauthorized | 401                |
