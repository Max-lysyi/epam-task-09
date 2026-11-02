@level3
Feature: Level 3 - Remove Guest (UI or API)
  As the Owner of a group reservation
  I want to remove a guest from the group pre-order
  So that their dishes are excluded and the total is recalculated accurately

  @api @ui @smoke @happy_path
  Scenario: Owner removes one guest from a group and total is recalculated
    Given an active group reservation exists with participants:
      | Name     | Type  | IsSubmitted | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | Spring Salad | 12.00 | 1        |
      | Alex     | Guest | true        | Tom Yum      | 22.00 | 2        |
      | Olena    | Guest | true        | Caesar Salad | 18.00 | 1        |
    When the Owner opens the group pre-order review page
    Then the Grand Total equals 74.00
    When the Owner removes the guest "Alex" via UI
    Then the guest "Alex" is no longer visible in the participants list
    And the remaining participants "Yevhenii" and "Olena" are still present
    And the Grand Total equals 30.00

  @api @edge_case
  Scenario: Remove a participant who has already confirmed their cart
    Given an active group reservation exists with participants:
      | Name     | Type  | IsSubmitted | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | Spring Salad | 12.00 | 1        |
      | Olena    | Guest | true        | Caesar Salad | 18.00 | 2        |
    When the Owner removes the guest session for "Olena" via API
    Then the API response status code is 200
    And the API response does not contain participant "Olena"
    And the API response total is 12.00

  @api @edge_case
  Scenario: Remove the last guest participant leaving only the Owner
    Given an active group reservation exists with only one guest "Alex" and the Owner:
      | Name     | Type  | IsSubmitted | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | Spring Salad | 12.00 | 1        |
      | Alex     | Guest | true        | Tom Yum      | 22.00 | 1        |
    When the Owner removes the guest session for "Alex" via API
    Then the API response status code is 200
    And the API response contains only 1 participant "Yevhenii"
    And the API response total is 12.00

  @api @negative
  Scenario: Attempting to remove a participant from a different group returns 400
    Given two separate group reservations "GroupA" and "GroupB" exist with guests
    When the Owner attempts to delete a guest from "GroupB" using "GroupA" endpoint
    Then the API response status code is 400

  @api @negative
  Scenario: Attempting to remove a non-existent participant returns 404
    Given a valid group reservation exists
    When the Owner attempts to delete a non-existent session "non-existent-session-404" via API
    Then the API response status code is 404

  @api @authorization
  Scenario Outline: Only the Owner can remove a guest session
    Given an active group reservation exists with a guest "Olena"
    When a user with role "<Role>" attempts to delete the guest "Olena" via API
    Then the API response status code is <ExpectedStatusCode>

    Examples:
      | Role         | ExpectedStatusCode |
      | Owner        | 200                |
      | Guest        | 403                |
      | Waiter       | 403                |
      | OtherClient  | 403                |
      | Unauthorized | 401                |

