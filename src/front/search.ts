/// <reference path="../../typings/main.d.ts" />
"use strict";
import Vue = require("vue");
import * as request from "superagent";

const baseUrl =
  function() {
    let u = window.location.href.split("/");
    u.pop();
    return u.join("/");
  }();

function boolToStatus(value: boolean): string {
  return value ? "enabled" : "disabled";
}

let app = new Vue({
  el: "#app",
  data: {
    query: undefined,
    strict: true,
    similarity: false,
    search_results: []
  },
  methods: {
    search: function() {
      request
        .get(baseUrl + "/api/search")
        .query({
          query: this.query,
          strict: boolToStatus(this.strict),
          similarity: boolToStatus(this.similarity)
        })
        .end((err, res) => {
          if (err || !res.ok) {
          } else {
            this.search_results = JSON.parse(res.text).values;
          }
        });
    }
  }
});