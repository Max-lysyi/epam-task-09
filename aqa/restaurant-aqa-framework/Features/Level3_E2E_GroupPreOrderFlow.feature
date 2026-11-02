@level3 @e2e
Feature: Level 3 - Complete End-to-End Group Pre-order Flow
  As a restaurant system
  I want to ensure the entire flow from guests adding dishes, Owner review, guest removal, submit, and Waiter fulfillment works flawlessly

  @smoke @happy_path
  Scenario: Full E2E flow: Guests add dishes -> Owner reviews -> Owner removes one guest -> Owner submits -> Waiter sees verified order
    Given an active group reservation is created with an order
    And the following participants join and add dishes:
      | Name     | Type  | IsSubmitted | DishId | DishName     | Price | Quantity |
      | Yevhenii | Owner | true        | dish1  | Caesar Salad | 18.00 | 1        |
      | Alex     | Guest | true        | dish2  | Tom Yum      | 22.00 | 2        |
      | Olena    | Guest | true        | dish1  | Caesar Salad | 18.00 | 2        |
    When the Owner opens the group pre-order review page
    Then the Owner sees all 3 participants in the list
    And the Grand Total equals 98.00
    When the Owner removes the guest "Alex" via UI
    Then the guest "Alex" is no longer visible in the participants list
    And the Grand Total equals 54.00
    When the Owner submits the consolidated pre-order via UI
    Then the group status badge changes to "Sent to Restaurant"
    And the Waiter verifies the final submitted order via API:
      | DishId | Quantity |
      | dish1  | 3        |
    And the Waiter verifies the removed guest dish "dish2" is not in the order

